using System.IO.Pipelines;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;

namespace NexArc.InterfaceBridge.Server;

public static partial class RouteMapper
{
    public static void Map(WebApplication app, Type managerType, MethodInfo method,
        JsonSerializerOptions jsonSerializerOptions, Type? managerImplementationType = null)
    {
        var connector = managerType.GetCustomAttribute<RestConnectorAttribute>();
        var rest = method.GetCustomAttribute<RestAttribute>()
                   ?? throw new InvalidOperationException(
                       $"Missing RestAttribute on method {managerType.FullName}.{method.Name}");

        var pattern = rest.Route.StartsWith('/')
            ? rest.Route.Trim('/')
            : $"{connector?.RoutePrefix ?? ""}/{rest.Route}".Trim('/');

        var httpMethod = new[] { rest.Method.ToString() };
        var parameters = method.GetParameters();
        var returnType = method.ReturnType;

        var parameterParsers = BuildParameterList(jsonSerializerOptions, parameters);

        var endpointBuilder = app.MapMethods(pattern, httpMethod, (Func<HttpContext, Task>)Handler);
        ApplyAuthorizationMetadata(endpointBuilder, managerType, method, managerImplementationType);
        return;

        // This should work like a Controller, where the method definition is used and an instance of TManagerInterface is created in scope of the request
        async Task Handler(HttpContext context)
        {
            var manager = context.RequestServices.GetRequiredService(managerType);

            var values = new Dictionary<string, string?>(8);

            // Extract form fields (the lowest priority)
            if (context.Request.HasFormContentType)
                foreach (var (key, value) in context.Request.Form)
                    values[key] = value;

            // Extract query fields
            foreach (var (key, value) in context.Request.Query) values[key] = value;

            // Extract route fields (the top priority)
            foreach (var (key, value) in context.GetRouteData().Values) values[key] = value?.ToString();

            var arguments = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.ParameterType == typeof(CancellationToken))
                {
                    arguments[i] = context.RequestAborted;
                    continue;
                }

                if (parameter.ParameterType == typeof(FilePart) && context.Request.HasFormContentType)
                {
                    var file = context.Request.Form.Files[parameter.Name!] ??
                               throw new BadHttpRequestException($"File expected for {parameter.Name}");
                    arguments[i] = FormFileToFilePart(file);
                    continue;
                }

                if (values.TryGetValue(parameter.Name!, out var value) && value is not null)
                {
                    arguments[i] = parameterParsers[i](value);
                }
            }

            // Call the method
            Task task;

            // Await
            try
            {
                task = (Task)method.Invoke(manager, arguments)!;
                await task;
            }
            catch (HttpResponseException ex)
            {
                await SetResponseFromException(context, ex);
                return;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is HttpResponseException httpResponseException)
            {
                await SetResponseFromException(context, httpResponseException);
                return;
            }
            
            // If Task<TResult>
            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var taskResult = returnType.GetProperty("Result")!.GetValue(task);

                await SetResponseContent(jsonSerializerOptions, taskResult, context);
                return;
            }

            // Otherwise return OK
            context.Response.StatusCode = 204;
        }
    }

    private static void ApplyAuthorizationMetadata(RouteHandlerBuilder endpointBuilder, Type managerType,
        MethodInfo interfaceMethod, Type? managerImplementationType)
    {
        var authorizeData = new List<IAuthorizeData>();
        var hasAllowAnonymous = false;

        AddAuthorizationMetadata(managerType, authorizeData, ref hasAllowAnonymous);
        AddAuthorizationMetadata(interfaceMethod, authorizeData, ref hasAllowAnonymous);

        if (managerImplementationType is not null)
        {
            AddAuthorizationMetadata(managerImplementationType, authorizeData, ref hasAllowAnonymous);
            var implementationMethod = ResolveImplementationMethod(managerType, interfaceMethod, managerImplementationType);
            if (implementationMethod is not null)
                AddAuthorizationMetadata(implementationMethod, authorizeData, ref hasAllowAnonymous);
        }

        if (hasAllowAnonymous)
        {
            endpointBuilder.AllowAnonymous();
            return;
        }

        if (authorizeData.Count > 0)
            endpointBuilder.RequireAuthorization(authorizeData.ToArray());
    }

    private static void AddAuthorizationMetadata(MemberInfo member, List<IAuthorizeData> authorizeData,
        ref bool hasAllowAnonymous)
    {
        foreach (var attribute in member.GetCustomAttributes(true))
        {
            if (attribute is IAllowAnonymous)
                hasAllowAnonymous = true;

            if (attribute is IAuthorizeData authorize)
                authorizeData.Add(authorize);
        }
    }

    private static MethodInfo? ResolveImplementationMethod(Type managerType, MethodInfo interfaceMethod,
        Type managerImplementationType)
    {
        if (managerType.IsInterface && managerType.IsAssignableFrom(managerImplementationType))
        {
            var map = managerImplementationType.GetInterfaceMap(managerType);
            for (var i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.InterfaceMethods[i] == interfaceMethod)
                    return map.TargetMethods[i];
            }
        }

        var parameterTypes = interfaceMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        return managerImplementationType.GetMethod(interfaceMethod.Name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
    }

    private static async Task SetResponseFromException(HttpContext context, HttpResponseException ex)
    {
        context.Response.StatusCode = (int)ex.Response.StatusCode;
        if (ex.Response.Content.Headers.ContentType is not null)
            context.Response.ContentType = ex.Response.Content.Headers.ContentType.ToString();
        var stream = await ex.Response.Content.ReadAsByteArrayAsync(CancellationToken.None);
        await context.Response.BodyWriter.WriteAsync(stream, CancellationToken.None);
    }

    private static async Task SetResponseContent(JsonSerializerOptions jsonSerializerOptions, object? result, HttpContext context)
    {
        if (result is FilePart filePart)
        {
            context.Response.ContentType = filePart.ContentType;
            context.Response.ContentLength = filePart.Length;
            await filePart.Content.CopyToAsync(context.Response.BodyWriter);
        }
        else
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(result, jsonSerializerOptions);
        }
    }

    private static Func<string, object?>[] BuildParameterList(JsonSerializerOptions jsonSerializerOptions,
        ParameterInfo[] parameters)
    {
        return parameters.Select<ParameterInfo, Func<string, object?>>(parameter =>
        {
            var type = parameter.ParameterType;
            if (type == typeof(string))
                return value => value;

            if (type.IsPrimitive)
                return value => (object?)Convert.ChangeType(value, parameter.ParameterType);

            if (type.IsEnum)
                return value => Enum.Parse(type, value);

            if (type.IsGenericType
                && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                && Nullable.GetUnderlyingType(type)!.IsPrimitive)
                return value => string.IsNullOrWhiteSpace(value)
                    ? Activator.CreateInstance(type)
                    : (object?)Convert.ChangeType(value, Nullable.GetUnderlyingType(type)!);

            var parseMethod = type.GetMethod("Parse",
                BindingFlags.Static | BindingFlags.Public, null, [typeof(string)], null);
            if (parseMethod is not null)
                return value => parseMethod.Invoke(null, [value]);

            return value =>
            {
                try
                {
                    return JsonSerializer.Deserialize(value, type, jsonSerializerOptions);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to parse parameter {0} of type {1}: {2}", parameter.Name, type.FullName,
                        value);
                    throw;
                }
            };
        }).ToArray();
    }

    private static FilePart FormFileToFilePart(IFormFile file)
    {
        return new FilePart
        {
            ContentType = file.ContentType,
            Content = file.OpenReadStream(),
            Length = file.Length,
            FileName = file.FileName,
            ETag = file.Headers.ETag,
            LastModifiedUtc = LastModifiedToDateTime(file.Headers.LastModified)
        };
    }

    private static DateTime? LastModifiedToDateTime(StringValues lastModified)
    {
        var value = lastModified.FirstOrDefault();
        if (string.IsNullOrEmpty(value)) return null;
        return DateTime.TryParse(value, out var dateTime) ? dateTime.ToUniversalTime() : null;
    }
}
