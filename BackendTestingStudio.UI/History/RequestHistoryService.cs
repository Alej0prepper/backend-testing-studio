using BackendTestingStudio.Core.History;
using BackendTestingStudio.Core.Environments;
using BackendTestingStudio.Core.Http;
using IEnvironmentService = BackendTestingStudio.Core.Environments.IEnvironmentService;

namespace BackendTestingStudio.UI.History;

internal sealed class RequestHistoryService : IRequestHistoryService
{
    private readonly IRequestHistoryRepository _repository;
    private readonly IHttpEngine _httpEngine;
    private readonly IEnvironmentService _environmentService;

    public RequestHistoryService(IRequestHistoryRepository repository, IHttpEngine httpEngine, IEnvironmentService environmentService)
    {
        _repository = repository;
        _httpEngine = httpEngine;
        _environmentService = environmentService;
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

        var environment = entry.EnvironmentId is null
            ? null
            : await _environmentService.GetByIdAsync(entry.EnvironmentId.Value, cancellationToken).ConfigureAwait(false);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await ExecuteAsync(
                entry.Request.Method,
                entry.Request.ToHttpRequestDefinition(
                    environment?.Authentication,
                    environment?.Variables.ToDictionary(item => item.Name, item => item.Value, StringComparer.OrdinalIgnoreCase) as IReadOnlyDictionary<string, string?>),
                cancellationToken)
            .ConfigureAwait(false);
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
