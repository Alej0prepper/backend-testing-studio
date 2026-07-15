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
        => ToHttpRequestDefinition(authentication, null);

    public HttpRequestDefinition ToHttpRequestDefinition(
        EnvironmentAuthentication? authentication,
        IReadOnlyDictionary<string, string?>? variables)
        => new(
            new Uri(HttpRequestTemplateResolver.ResolveText(Url, variables), UriKind.Absolute),
            HttpRequestTemplateResolver.ResolveDictionary(Headers, variables),
            body: BuildBody(variables),
            authentication: HttpRequestTemplateResolver.ResolveAuthentication(authentication.ToHttpAuthentication(), variables),
            variables: variables);

    private HttpRequestBody? BuildBody(IReadOnlyDictionary<string, string?>? variables = null)
        => BodyKind switch
        {
            RequestHistoryBodyKind.None => null,
            RequestHistoryBodyKind.Json => new HttpRequestBody.RawJson(HttpRequestTemplateResolver.ResolveText(JsonBody, variables)),
            RequestHistoryBodyKind.Multipart => new HttpRequestBody.Multipart(
                MultipartParts.Select(part => new HttpMultipartPart(
                        HttpRequestTemplateResolver.ResolveText(part.Name, variables),
                        Convert.FromBase64String(HttpRequestTemplateResolver.ResolveText(part.Value, variables)),
                        HttpRequestTemplateResolver.ResolveNullableText(part.FileName, variables),
                        HttpRequestTemplateResolver.ResolveNullableText(part.ContentType, variables)))
                    .ToArray()),
            _ => throw new NotSupportedException($"Body kind '{BodyKind}' is not supported.")
        };
}
