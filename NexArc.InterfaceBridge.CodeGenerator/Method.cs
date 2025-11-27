using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace NexArc.InterfaceBridge.CodeGenerator;

public class Method
{
    private const string RestAttributeName = "RestAttribute";
    private const string FilePartName = "global::NexArc.InterfaceBridge.FilePart";

    public string? ReturnType { get; }
    public string? ReturnTypeSerializer { get; }
    public BodyType RequestBodyType { get; }
    public IMethodSymbol MethodSymbol { get; }
    public Bridge Bridge { get; }
    public HttpMethod HttpMethod { get; }
    public string Route { get; }
    public bool IsReturnsFile { get; }
    public bool IsAcceptsFile { get; }
    public Argument[] Parameters { get; }
    public bool ReturnTypeNullProtection { get; }

    public Method(IMethodSymbol methodSymbol, Bridge bridge)
    {
        var restAttribute = methodSymbol.GetAttributes().Single(x => x.AttributeClass!.Name == RestAttributeName);
        HttpMethod = (HttpMethod)restAttribute.ConstructorArguments[0].Value!;
        Route = SimplifyRouteArguments(
            (bridge.RoutePrefix is null ? "" : $"{bridge.RoutePrefix}/")
            + (string)restAttribute.ConstructorArguments[1].Value!);
        RequestBodyType =
            restAttribute.NamedArguments.FirstOrDefault(x => x.Key == "BodyType").Value.Value as BodyType? ??
            BodyType.Auto;
        MethodSymbol = methodSymbol;
        Bridge = bridge;

        var returnType = (INamedTypeSymbol?)methodSymbol.ReturnType;
        if (returnType!.Name != "Task")
            throw new InvalidOperationException("Only Task methods are supported.");
        var resultType = returnType?.IsGenericType == true
            ? returnType.TypeArguments[0]
            : null;
        ReturnType = resultType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        ReturnTypeSerializer = BuildSerializer(resultType);
        ReturnTypeNullProtection = resultType is
            { NullableAnnotation: NullableAnnotation.NotAnnotated, IsValueType: false };

        IsAcceptsFile = methodSymbol.Parameters.Any(x =>
            x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == FilePartName);
        IsReturnsFile = ReturnType == FilePartName;

        if (RequestBodyType == BodyType.Auto)
            RequestBodyType = IsAcceptsFile ? BodyType.MultipartFormData : BodyType.FormUrlEncoded;

        if (IsAcceptsFile && RequestBodyType != BodyType.MultipartFormData)
            throw new InvalidOperationException(
                "FilePart parameters require BodyType.Auto or BodyType.MultipartFormData.");

        var routeParameters = ExtractRouteArguments(Route);
        Parameters = methodSymbol.Parameters.Select(x => new Argument(this, x, routeParameters)).ToArray();
    }

    private static string SimplifyRouteArguments(string routeTemplate) =>
        Regex.Replace(routeTemplate, @"\{([^\}:]+)(?::[^\}]*)?\}", "{$1}");

    private static HashSet<string> ExtractRouteArguments(string routeTemplate) =>
        new HashSet<string>(
            Regex.Matches(routeTemplate, @"\{([^\}:]+)(?::[^\}]*)?\}")
                .Cast<Match>()
                .Where(x => x.Groups.Count > 1)
                .Select(x => x.Groups[1].Value));

    private static readonly SymbolDisplayFormat MethodFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                         SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                         SymbolDisplayGenericsOptions.IncludeVariance,
        memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeModifiers |
                       SymbolDisplayMemberOptions.IncludeType,
        parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName |
                          SymbolDisplayParameterOptions.IncludeDefaultValue |
                          SymbolDisplayParameterOptions.IncludeParamsRefOut,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                              SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );

    public void BuildSource(StringBuilder sb)
    {
        sb.AppendLine($"    public async {MethodSymbol.ToDisplayString(MethodFormat)}");
        sb.AppendLine("    {");
        BuildRequest(sb);
        BuildResponse(sb);
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void BuildRequest(StringBuilder sb)
    {
        var hasRouteParameters = Parameters.Any(x => x.IsInRoute);
        var hasFileParameters = Parameters.Any(x => x.IsFilePart);
        var hasBodyParameters = Parameters.Any(x => !x.IsInRoute && !x.IsFilePart);

        sb.Append(
                "        var _request = new global::System.Net.Http.HttpRequestMessage(global::System.Net.Http.HttpMethod.")
            .AppendLine(hasRouteParameters
                ? $"{HttpMethod}, $\"{Route}\");"
                : $"{HttpMethod}, \"{Route}\");");

        if (hasBodyParameters || hasFileParameters)
        {
            if (RequestBodyType == BodyType.FormUrlEncoded)
            {
                sb.AppendLine("        _request.Content = new global::System.Net.Http.FormUrlEncodedContent([");
                AppendFormUrlEncodedContent("            ");
                sb.AppendLine("        ]);");
            }
            else if (RequestBodyType == BodyType.MultipartFormData)
            {
                sb.AppendLine("        var _content = new global::System.Net.Http.MultipartFormDataContent();");

                foreach (var parameter in Parameters.Where(x => x is { IsFilePart: false, IsInRoute: false, IsCancellationToken: false }))
                    sb.Append("        ")
                        .AppendLine(
                            $"_content.Add(new global::System.Net.Http.StringContent({parameter.Serializer}), \"{parameter.Name}\");");

                if (hasFileParameters)
                {
                    foreach (var parameter in Parameters.Where(x => x.IsFilePart).Select(x => x.Name))
                    {
                        sb.AppendLine(
                            $$"""
                                      var _{{parameter}} = new StreamContent({{parameter}}.Content);
                                      if (!string.IsNullOrWhiteSpace({{parameter}}.ContentType))
                                          _{{parameter}}.Headers.ContentType = global::System.Net.Http.Headers.MediaTypeHeaderValue.Parse({{parameter}}.ContentType);
                                      _{{parameter}}.Headers.ContentLength = {{parameter}}.Length;
                                      _content.Add(_{{parameter}}, "{{parameter}}", {{parameter}}.FileName ?? "{{parameter}}");
                              """);
                    }
                }

                sb.AppendLine("        request.Content = _content;");
            }
        }

        return;

        void AppendFormUrlEncodedContent(string indent)
        {
            foreach (var parameter in Parameters.Where(x => x is { IsFilePart: false, IsInRoute: false, IsCancellationToken: false }))
                sb.AppendLine(
                    $"{indent}new global::System.Collections.Generic.KeyValuePair<string, string?>(\"{parameter.Name}\", {parameter.Serializer}),");
        }
    }

    private void BuildResponse(StringBuilder sb)
    {
        var cancellationToken = Parameters.FirstOrDefault(x => x.IsCancellationToken)?.Name ?? "CancellationToken.None";

        sb.AppendLine($"        var _response = await this.HttpClient.SendAsync(_request, {cancellationToken});");
        sb.AppendLine("        _response.EnsureSuccessStatusCode();");

        if (string.IsNullOrWhiteSpace(ReturnType))
            return;

        if (ReturnType == ClientDefinition.FilePartTypeName)
        {
            sb.AppendLine(
                $$"""
                          return new {{ClientDefinition.FilePartTypeName}}()
                          {
                              ContentType = _response.Content.Headers.ContentType?.ToString(),
                              Content = new MemoryStream(await _response.Content.ReadAsByteArrayAsync({{cancellationToken}})),
                              FileName = _response.Content.Headers.ContentDisposition?.FileName?.Trim('"'),
                              Length = _response.Content.Headers.ContentLength
                          };
                  """);
        }
        else
        {
            sb.AppendLine($"        var _responseContent = await _response.Content.ReadAsStringAsync({cancellationToken});");
            sb.Append($"        return {ReturnTypeSerializer}".TrimEnd());
            if (ReturnTypeNullProtection)
                sb.AppendLine()
                    .AppendLine("            ?? throw new InvalidOperationException(\"Response was null.\");");
            else
                sb.AppendLine(";");
        }
    }

    private string? BuildSerializer(ITypeSymbol? returnSymbol)
    {
        if (returnSymbol is null || string.IsNullOrEmpty(ReturnType)) return null;

        if (ReturnType == "string")
            return "_responseContent";
        
        if (string.IsNullOrEmpty(Bridge.JsonSerializerContext)) 
            return $"global::System.Text.Json.JsonSerializer.Deserialize<{ReturnType}>(_responseContent)";

        if (!ClientDefinition.BuildInTypes.Contains(ReturnType!.TrimEnd('?')))
        {
            if (ReturnType.StartsWith(ClientDefinition.JsonPatchDocumentTypeName))
                return
                    $"global::System.Text.Json.JsonSerializer.Deserialize<{ReturnType}>(_responseContent, {Bridge.JsonSerializerContext}.Default.JsonPatchDocument)";

            if (returnSymbol.Kind == SymbolKind.ArrayType &&
                returnSymbol is IArrayTypeSymbol arrayTypeSymbol)
                return
                    $"global::System.Text.Json.JsonSerializer.Deserialize<{ReturnType}>(_responseContent, {Bridge.JsonSerializerContext}.Default.{arrayTypeSymbol.ElementType.Name}Array)";

            return
                $"global::System.Text.Json.JsonSerializer.Deserialize<{ReturnType}>(_responseContent, {Bridge.JsonSerializerContext}.Default.{returnSymbol.Name})";
        }

        return $"global::System.Text.Json.JsonSerializer.Deserialize<{ReturnType}>(_responseContent, {Bridge.JsonSerializerContext}.Default.Options)";
    }
}