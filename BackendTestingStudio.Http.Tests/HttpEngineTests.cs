using System.Net;
using System.Net.Http.Headers;
using BackendTestingStudio.Core.Http;
using BackendTestingStudio.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BackendTestingStudio.Http.Tests;

public sealed class HttpEngineTests
{
    [Fact]
    public void AddBackendTestingStudioHttp_RegistersIHttpEngine()
    {
        var services = new ServiceCollection();

        services.AddBackendTestingStudioHttp();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IHttpEngine>());
    }

    [Fact]
    public async Task GetAsync_ComposesQueryHeadersAndBearerAuth()
    {
        var handler = new RecordingHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("pong")
        });
        var engine = CreateEngine(handler);

        var response = await engine.GetAsync(
            new HttpRequestDefinition(
                new Uri("https://api.example.test/items"),
                headers: new Dictionary<string, string?> { ["X-Client"] = "bts" },
                queryParameters: new Dictionary<string, string?> { ["search"] = "alpha" },
                authentication: new HttpAuthentication.Bearer("token-123")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("pong", response.Content);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://api.example.test/items?search=alpha", handler.LastRequest!.Uri.ToString());
        Assert.Equal("bts", handler.LastRequest!.Headers["X-Client"].Single());
        Assert.Equal("Bearer", handler.LastRequest!.Authorization?.Scheme);
        Assert.Equal("token-123", handler.LastRequest!.Authorization?.Parameter);
    }

    [Fact]
    public async Task PostAsync_SendsJsonPayload()
    {
        var handler = new RecordingHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("created")
        });
        var engine = CreateEngine(handler);

        var response = await engine.PostAsync(
            new HttpRequestDefinition(
                new Uri("https://api.example.test/orders"),
                body: new HttpRequestBody.Json(new { name = "alpha", quantity = 3 })));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("created", response.Content);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.StartsWith("application/json", handler.LastRequest!.ContentType);
        Assert.Equal("""{"name":"alpha","quantity":3}""", handler.LastRequest!.Body);
    }

    [Fact]
    public async Task PutAsync_SendsBasicAuth()
    {
        var handler = new RecordingHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.NoContent));
        var engine = CreateEngine(handler);

        await engine.PutAsync(
            new HttpRequestDefinition(
                new Uri("https://api.example.test/profile"),
                authentication: new HttpAuthentication.Basic("demo", "secret")));

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Equal("Basic", handler.LastRequest!.Authorization?.Scheme);
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("demo:secret")), handler.LastRequest!.Authorization?.Parameter);
    }

    [Fact]
    public async Task PatchAsync_SendsMultipartAndApiKey()
    {
        var handler = new RecordingHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.OK));
        var engine = CreateEngine(handler);

        await engine.PatchAsync(
            new HttpRequestDefinition(
                new Uri("https://api.example.test/upload"),
                authentication: new HttpAuthentication.ApiKey("X-Api-Key", "key-456"),
                body: new HttpRequestBody.Multipart(
                    [
                        HttpMultipartPart.Text("title", "sample"),
                        HttpMultipartPart.File("document", [1, 2, 3], "document.bin", "application/octet-stream")
                    ])));

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.Contains("multipart/form-data", handler.LastRequest!.ContentType);
        Assert.Contains("title", handler.LastRequest!.Body);
        Assert.Contains("sample", handler.LastRequest!.Body);
        Assert.Contains("document.bin", handler.LastRequest!.Body);
        Assert.Equal("key-456", handler.LastRequest!.Headers["X-Api-Key"].Single());
    }

    [Fact]
    public async Task DeleteAsync_SendsDeleteRequest()
    {
        var handler = new RecordingHttpMessageHandler(() => new HttpResponseMessage(HttpStatusCode.Accepted));
        var engine = CreateEngine(handler);

        await engine.DeleteAsync(
            new HttpRequestDefinition(
                new Uri("https://api.example.test/items/42"),
                queryParameters: new Dictionary<string, string?> { ["hard"] = "true" }));

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal("https://api.example.test/items/42?hard=true", handler.LastRequest!.Uri.ToString());
    }

    private static IHttpEngine CreateEngine(RecordingHttpMessageHandler handler)
        => new HttpEngine(new HttpClient(handler));

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responseFactory;

        public RecordingHttpMessageHandler(Func<HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public CapturedRequest? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in request.Headers)
            {
                headers[header.Key] = header.Value.ToArray();
            }

            string? body = null;
            string? contentType = null;

            if (request.Content is not null)
            {
                foreach (var header in request.Content.Headers)
                {
                    headers[header.Key] = header.Value.ToArray();
                }

                contentType = request.Content.Headers.ContentType?.ToString();
                body = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            LastRequest = new CapturedRequest(
                request.Method,
                request.RequestUri ?? throw new InvalidOperationException("Request URI was not set."),
                headers,
                body,
                contentType,
                request.Headers.Authorization);

            return _responseFactory();
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri Uri,
        IReadOnlyDictionary<string, string[]> Headers,
        string? Body,
        string? ContentType,
        AuthenticationHeaderValue? Authorization);
}
