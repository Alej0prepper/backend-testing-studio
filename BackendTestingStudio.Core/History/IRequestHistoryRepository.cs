namespace BackendTestingStudio.Core.History;

public interface IRequestHistoryRepository
{
    Task<IReadOnlyList<RequestHistoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RequestHistoryEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RequestHistoryEntry> AddAsync(RequestHistoryEntry entry, CancellationToken cancellationToken = default);
}
