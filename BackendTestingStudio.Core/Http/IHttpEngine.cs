namespace BackendTestingStudio.Core.Http;

public interface IHttpEngine
{
    Task<HttpResponseResult> GetAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default);

    Task<HttpResponseResult> PostAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default);

    Task<HttpResponseResult> PutAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default);

    Task<HttpResponseResult> PatchAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default);

    Task<HttpResponseResult> DeleteAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default);
}
