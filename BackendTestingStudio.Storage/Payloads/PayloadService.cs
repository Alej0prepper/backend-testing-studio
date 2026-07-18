using BackendTestingStudio.Core.Payloads;

namespace BackendTestingStudio.Storage.Payloads;

internal sealed class PayloadService : IPayloadService
{
    private readonly IPayloadRepository _repository;

    public PayloadService(IPayloadRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<PayloadDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<PayloadDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<PayloadDefinition> CreateAsync(PayloadDefinition payload, CancellationToken cancellationToken = default)
        => _repository.CreateAsync(payload, cancellationToken);

    public Task<PayloadDefinition> UpdateAsync(PayloadDefinition payload, CancellationToken cancellationToken = default)
        => _repository.UpdateAsync(payload, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);
}
