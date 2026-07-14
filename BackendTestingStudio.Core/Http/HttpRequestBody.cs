namespace BackendTestingStudio.Core.Http;

public abstract record HttpRequestBody
{
    protected HttpRequestBody()
    {
    }

    public sealed record Json(object? Value, string? ContentType = "application/json") : HttpRequestBody;

    public sealed record Multipart(IReadOnlyList<HttpMultipartPart> Parts) : HttpRequestBody;
}
