namespace NexArc.InterfaceBridge;

/// <summary>
/// Represents a file part used in multipart form data uploads.
/// This class contains metadata and the content stream associated with the file.
/// </summary>
/// <remarks>
/// The <see cref="FilePart"/> class is used to encapsulate file properties such as file name,
/// MIME type, content length, and the stream of the file itself. This is commonly needed
/// for scenarios involving handling file uploads in network requests or similar processes.
/// </remarks>
/// <property name="FileName">
/// Gets or sets the name of the file.
/// </property>
/// <property name="ContentType">
/// Gets or sets the MIME type of the file content.
/// </property>
/// <property name="Length">
/// Gets or sets the size of the file in bytes.
/// </property>
/// <property name="Content">
/// Gets or sets the stream representing the content of the file.
/// </property>
public sealed class FilePart : IDisposable, IAsyncDisposable
{
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? Length { get; set; }
    public Stream Content { get; set; } = Stream.Null;

    public void Dispose()
    {
        Content.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync();
    }
}