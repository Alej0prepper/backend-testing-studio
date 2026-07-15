namespace BackendTestingStudio.Core.Payloads;

public interface IPayloadRepository
{
    Task<IReadOnlyList<PayloadDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PayloadDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PayloadDefinition> CreateAsync(PayloadDefinition payload, CancellationToken cancellationToken = default);

    Task<PayloadDefinition> UpdateAsync(PayloadDefinition payload, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
