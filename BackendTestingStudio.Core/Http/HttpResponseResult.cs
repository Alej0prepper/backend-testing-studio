using System.Net;

namespace BackendTestingStudio.Core.Http;

public sealed record HttpResponseResult
{
    public HttpResponseResult(
        HttpStatusCode statusCode,
        string? content,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        StatusCode = statusCode;
        Content = content;
        Headers = headers;
    }

    public HttpStatusCode StatusCode { get; }

    public string? Content { get; }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; }
}
