using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BackendTestingStudio.Core.Http;

namespace BackendTestingStudio.Http;

public sealed class HttpEngine : IHttpEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public HttpEngine(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseResult> GetAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, request, cancellationToken);

    public Task<HttpResponseResult> PostAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, request, cancellationToken);

    public Task<HttpResponseResult> PutAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Put, request, cancellationToken);

    public Task<HttpResponseResult> PatchAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Patch, request, cancellationToken);

    public Task<HttpResponseResult> DeleteAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Delete, request, cancellationToken);

    private async Task<HttpResponseResult> SendAsync(HttpMethod method, HttpRequestDefinition request, CancellationToken cancellationToken)
    {
        var requestUri = BuildRequestUri(request.Url, request.QueryParameters);
        using var message = new HttpRequestMessage(method, requestUri);

        ApplyHeaders(message, request.Headers);
        ApplyAuthentication(message, request.Authentication);
        ApplyBody(message, request.Body);

        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var content = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        return new HttpResponseResult(
            response.StatusCode,
            content,
            CollectHeaders(response));
    }

    private static Uri BuildRequestUri(Uri url, IReadOnlyDictionary<string, string?>? queryParameters)
    {
        var raw = url.ToString();
        var fragmentIndex = raw.IndexOf('#');
        var fragment = fragmentIndex >= 0 ? raw[fragmentIndex..] : string.Empty;
        var pathAndQuery = fragmentIndex >= 0 ? raw[..fragmentIndex] : raw;

        var queryIndex = pathAndQuery.IndexOf('?');
        var path = queryIndex >= 0 ? pathAndQuery[..queryIndex] : pathAndQuery;
        var existingQuery = queryIndex >= 0 ? pathAndQuery[(queryIndex + 1)..] : string.Empty;

        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(existingQuery))
        {
            queryParts.Add(existingQuery);
        }

        if (queryParameters is not null)
        {
            foreach (var pair in queryParameters)
            {
                var encodedKey = Uri.EscapeDataString(pair.Key);
                var encodedValue = Uri.EscapeDataString(pair.Value ?? string.Empty);
                queryParts.Add($"{encodedKey}={encodedValue}");
            }
        }

        var query = queryParts.Count == 0 ? string.Empty : "?" + string.Join("&", queryParts);
        return new Uri(path + query + fragment, UriKind.RelativeOrAbsolute);
    }

    private static void ApplyHeaders(HttpRequestMessage requestMessage, IReadOnlyDictionary<string, string?>? headers)
    {
        if (headers is null)
        {
            return;
        }

        foreach (var header in headers)
        {
            requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    private static void ApplyAuthentication(HttpRequestMessage requestMessage, HttpAuthentication? authentication)
    {
        switch (authentication)
        {
            case null:
                return;
            case HttpAuthentication.Bearer bearer:
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer.Token);
                return;
            case HttpAuthentication.Basic basic:
            {
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{basic.UserName}:{basic.Password}"));
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
                return;
            }
            case HttpAuthentication.ApiKey apiKey:
                requestMessage.Headers.TryAddWithoutValidation(apiKey.HeaderName, apiKey.Value);
                return;
            default:
                throw new NotSupportedException($"Authentication type '{authentication.GetType().Name}' is not supported.");
        }
    }

    private static void ApplyBody(HttpRequestMessage requestMessage, HttpRequestBody? body)
    {
        switch (body)
        {
            case null:
                return;
            case HttpRequestBody.Json json:
            {
                var jsonText = JsonSerializer.Serialize(json.Value, JsonOptions);
                var content = new StringContent(jsonText, Encoding.UTF8, json.ContentType ?? "application/json");
                requestMessage.Content = content;
                return;
            }
            case HttpRequestBody.Multipart multipart:
            {
                var content = new MultipartFormDataContent();
                foreach (var part in multipart.Parts)
                {
                    var partContent = new ByteArrayContent(part.Content);
                    if (!string.IsNullOrWhiteSpace(part.ContentType))
                    {
                        partContent.Headers.ContentType = MediaTypeHeaderValue.Parse(part.ContentType);
                    }

                    if (string.IsNullOrWhiteSpace(part.FileName))
                    {
                        content.Add(partContent, part.Name);
                    }
                    else
                    {
                        content.Add(partContent, part.Name, part.FileName);
                    }
                }

                requestMessage.Content = content;
                return;
            }
            default:
                throw new NotSupportedException($"Body type '{body.GetType().Name}' is not supported.");
        }
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CollectHeaders(HttpResponseMessage response)
    {
        var headers = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in response.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        if (response.Content is not null)
        {
            foreach (var header in response.Content.Headers)
            {
                headers[header.Key] = header.Value.ToArray();
            }
        }

        return headers;
    }
}
