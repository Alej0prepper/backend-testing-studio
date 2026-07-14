using BackendTestingStudio.Core.History;
using BackendTestingStudio.Core.Http;

namespace BackendTestingStudio.UI.History;

internal sealed class RequestHistoryService : IRequestHistoryService
{
    private readonly IRequestHistoryRepository _repository;
    private readonly IHttpEngine _httpEngine;

    public RequestHistoryService(IRequestHistoryRepository repository, IHttpEngine httpEngine)
    {
        _repository = repository;
        _httpEngine = httpEngine;
    }

    public Task<IReadOnlyList<RequestHistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<RequestHistoryEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<RequestHistoryEntry> RecordAsync(RequestHistoryEntry entry, CancellationToken cancellationToken = default)
        => _repository.AddAsync(entry, cancellationToken);

    public async Task<RequestHistoryEntry> RepeatAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entry is null)
        {
            throw new KeyNotFoundException($"History entry '{id}' was not found.");
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await ExecuteAsync(entry.Request.Method, entry.Request.ToHttpRequestDefinition(), cancellationToken).ConfigureAwait(false);
        stopwatch.Stop();

        var repeated = new RequestHistoryEntry(
            Guid.Empty,
            DateTimeOffset.UtcNow,
            entry.EnvironmentId,
            entry.EnvironmentName,
            entry.Request,
            new RequestHistoryResponseSnapshot(
                result.StatusCode,
                result.Headers,
                result.Content),
            stopwatch.Elapsed.TotalMilliseconds);

        return await _repository.AddAsync(repeated, cancellationToken).ConfigureAwait(false);
    }

    private Task<HttpResponseResult> ExecuteAsync(string method, HttpRequestDefinition request, CancellationToken cancellationToken)
        => method switch
        {
            var candidate when candidate.Equals("GET", StringComparison.OrdinalIgnoreCase) => _httpEngine.GetAsync(request, cancellationToken),
            var candidate when candidate.Equals("POST", StringComparison.OrdinalIgnoreCase) => _httpEngine.PostAsync(request, cancellationToken),
            var candidate when candidate.Equals("PUT", StringComparison.OrdinalIgnoreCase) => _httpEngine.PutAsync(request, cancellationToken),
            var candidate when candidate.Equals("PATCH", StringComparison.OrdinalIgnoreCase) => _httpEngine.PatchAsync(request, cancellationToken),
            var candidate when candidate.Equals("DELETE", StringComparison.OrdinalIgnoreCase) => _httpEngine.DeleteAsync(request, cancellationToken),
            _ => throw new NotSupportedException($"Method '{method}' is not supported.")
        };
}
