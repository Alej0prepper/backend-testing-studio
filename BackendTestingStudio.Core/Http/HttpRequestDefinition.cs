namespace BackendTestingStudio.Core.Http;

public sealed record HttpRequestDefinition
{
    public HttpRequestDefinition(
        Uri url,
        IReadOnlyDictionary<string, string?>? headers = null,
        IReadOnlyDictionary<string, string?>? queryParameters = null,
        HttpRequestBody? body = null,
        HttpAuthentication? authentication = null,
        IReadOnlyDictionary<string, string?>? variables = null)
    {
        ArgumentNullException.ThrowIfNull(url);

        Url = url;
        Headers = headers;
        QueryParameters = queryParameters;
        Body = body;
        Authentication = authentication;
        Variables = variables;
    }

    public Uri Url { get; }

    public IReadOnlyDictionary<string, string?>? Headers { get; }

    public IReadOnlyDictionary<string, string?>? QueryParameters { get; }

    public HttpRequestBody? Body { get; }

    public HttpAuthentication? Authentication { get; }

    public IReadOnlyDictionary<string, string?>? Variables { get; }
}
