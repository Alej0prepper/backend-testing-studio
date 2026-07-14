namespace BackendTestingStudio.Core.History;

public interface IRequestHistoryService
{
    Task<IReadOnlyList<RequestHistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RequestHistoryEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RequestHistoryEntry> RecordAsync(RequestHistoryEntry entry, CancellationToken cancellationToken = default);

    Task<RequestHistoryEntry> RepeatAsync(Guid id, CancellationToken cancellationToken = default);
}
