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
    public DateTime? LastModifiedUtc { get; set; }
    public string? ETag { get; set; }
    
    public void Dispose()
    {
        Content.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync();
    }

    public static FilePart CreateFromBase64(ReadOnlySpan<char> base64Content, string? fileName, string? contentType)
    {
        var maxLength = (int)Math.Ceiling(base64Content.Length / 3d) * 4;
        var stream = new MemoryStream(maxLength);

        if (Convert.TryFromBase64Chars(base64Content, stream.GetBuffer().AsSpan(), out var length))
        {
            stream.SetLength(length);
            return Create(stream, fileName, contentType);
        }

        return new() { FileName = fileName, ContentType = contentType };
    }

    public static FilePart CreateFromBase64(string base64Content, string? fileName, string? contentType) =>
        Create(Convert.FromBase64String(base64Content), fileName, contentType);

    public static FilePart Create(Stream content, string? fileName, string? contentType) => new()
    {
        Content = content,
        FileName = fileName,
        Length = content.Length,
        ContentType = contentType
    };

    private static Stream CopyStream(Stream stream)
    {
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }
    
    private static async Task<Stream> CopyStreamAsync(Stream stream)
    {
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }
    
    public static FilePart CreateCopy(Stream content, string? fileName, string? contentType) => 
        Create(CopyStream(content), fileName, contentType);

    public static async Task<FilePart> CreateCopyAsync(Stream content, string? fileName, string? contentType) => 
        Create(await CopyStreamAsync(content), fileName, contentType);

    public static FilePart Create(byte[] content, string? fileName, string? contentType) => new()
    {
        Content = new MemoryStream(content),
        FileName = fileName,
        Length = content.Length,
        ContentType = contentType
    };
}