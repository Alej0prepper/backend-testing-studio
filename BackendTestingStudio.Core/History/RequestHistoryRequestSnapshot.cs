using System.Text.Json;
using BackendTestingStudio.Core.Environments;
using BackendTestingStudio.Core.Http;

namespace BackendTestingStudio.Core.History;

public sealed record RequestHistoryRequestSnapshot
{
    public RequestHistoryRequestSnapshot(
        string method,
        string url,
        IReadOnlyDictionary<string, string?>? headers = null,
        RequestHistoryBodyKind bodyKind = RequestHistoryBodyKind.None,
        string? jsonBody = null,
        IReadOnlyList<RequestHistoryMultipartPart>? multipartParts = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        Method = method.Trim().ToUpperInvariant();
        Url = url.Trim();
        Headers = headers is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(headers, StringComparer.OrdinalIgnoreCase);
        BodyKind = bodyKind;
        JsonBody = jsonBody;
        MultipartParts = multipartParts ?? [];
    }

    public string Method { get; }

    public string Url { get; }

    public IReadOnlyDictionary<string, string?> Headers { get; }

    public RequestHistoryBodyKind BodyKind { get; }

    public string? JsonBody { get; }

    public IReadOnlyList<RequestHistoryMultipartPart> MultipartParts { get; }

    public HttpRequestDefinition ToHttpRequestDefinition(EnvironmentAuthentication? authentication = null)
        => new(
            new Uri(Url, UriKind.Absolute),
            Headers,
            body: BuildBody(),
            authentication: authentication.ToHttpAuthentication());

    private HttpRequestBody? BuildBody()
        => BodyKind switch
        {
            RequestHistoryBodyKind.None => null,
            RequestHistoryBodyKind.Json => string.IsNullOrWhiteSpace(JsonBody)
                ? new HttpRequestBody.Json(null)
                : new HttpRequestBody.Json(JsonDocument.Parse(JsonBody).RootElement.Clone()),
            RequestHistoryBodyKind.Multipart => new HttpRequestBody.Multipart(
                MultipartParts.Select(part => new HttpMultipartPart(
                        part.Name,
                        Convert.FromBase64String(part.Value),
                        part.FileName,
                        part.ContentType))
                    .ToArray()),
            _ => throw new NotSupportedException($"Body kind '{BodyKind}' is not supported.")
        };
}
