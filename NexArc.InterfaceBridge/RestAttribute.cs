using JetBrains.Annotations;

namespace NexArc.InterfaceBridge;

#pragma warning disable CS9113
/// <summary>
/// Associates a method with a RESTful HTTP request.
/// Provides information about the HTTP method and route template used for the request.
/// </summary>
/// <remarks>
/// GET methods will use Route first, then additional arguments will be provided in the query string.
/// POST, PATH, PUT methods will use Route first, then additional arguments will be provided in the body as "application/x-www-form-urlencoded"
/// unless FilePart is provided, then the "multipart/form-data" will be used.
/// </remarks>
/// <param name="method">
/// The HTTP method (e.g., GET, POST, PUT, DELETE) to be used for the RESTful request.
/// </param>
/// <param name="route">
/// The route template specifying the endpoint for the RESTful method.
/// This can include placeholders for parameters or represent a static URL path.
/// </param>
/// <param name="bodyType">
/// An optional parameter that represents the body type of the request for non-GET.
/// Auto will use MultipartFormData if FilePart is included, otherwise FormUrlEncoded.
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RestAttribute(HttpMethod method, [RouteTemplate] string route)
    : Attribute
{
    /// <summary>
    /// The route template specifying the endpoint for the RESTful method.
    /// This can include placeholders for parameters or represent a static URL path.
    /// </summary>
    [RouteTemplate]
    public string Route { get; } = route;

    /// <summary>
    /// The HTTP method (e.g., GET, POST, PUT, DELETE) to be used for the RESTful request.
    /// </summary>
    public HttpMethod Method { get; } = method;

    /// <summary>
    /// The body type of the request for non-GET.
    /// Auto will use MultipartFormData if FilePart is included, otherwise FormUrlEncoded.
    /// </summary>
    public BodyType BodyType { get; set; } = BodyType.Auto;
};

[AttributeUsage(AttributeTargets.Interface)]
public sealed class RestConnectorAttribute([RouteTemplate] string routePrefix) : Attribute
{
    [RouteTemplate] public string RoutePrefix { get; } = routePrefix;
}