using System.Text;

namespace BackendTestingStudio.Core.Http;

public sealed record HttpMultipartPart
{
    public HttpMultipartPart(string name, byte[] content, string? fileName = null, string? contentType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(content);

        Name = name;
        Content = content;
        FileName = fileName;
        ContentType = contentType;
    }

    public string Name { get; }

    public byte[] Content { get; }

    public string? FileName { get; }

    public string? ContentType { get; }

    public static HttpMultipartPart Text(string name, string value, string contentType = "text/plain; charset=utf-8")
        => new(name, Encoding.UTF8.GetBytes(value), null, contentType);

    public static HttpMultipartPart File(string name, byte[] content, string fileName, string contentType)
        => new(name, content, fileName, contentType);
}
