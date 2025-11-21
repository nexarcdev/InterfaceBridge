namespace NexArc.InterfaceBridge;

#pragma warning disable CS9113
/// <summary>
/// Specifies that an implementation of the provided bridge interface
/// should be generated for the partial class to which this attribute is applied.
/// Additionally, a JsonSerializerContext can be optionally provided to map
/// all the return models.
/// </summary>
/// <param name="bridgeInterface">
/// The interface type that should be implemented by the annotated class.
/// </param>
/// <param name="jsonSerializerContext">
/// An optional parameter that represents the context mapping all the return models
/// using a JSON serializer. If not provided, AOT will not be supported for iOS.
/// </param>
/// <param name="defaultBodyType">
/// An optional parameter that represents the body type of the request for non-GET.
/// Auto will use MultipartFormData if FilePart is included, otherwise FormUrlEncoded.
/// </param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class BridgeAttribute(Type bridgeInterface) : Attribute
{
    public Type? JsonSerializerContext { get; set; }
    public BodyType DefaultBodyType { get; set; }
};