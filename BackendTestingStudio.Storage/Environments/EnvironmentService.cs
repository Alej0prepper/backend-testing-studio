using BackendTestingStudio.Core.Environments;

namespace BackendTestingStudio.Storage.Environments;

internal sealed class EnvironmentService : IEnvironmentService
{
    private readonly IEnvironmentRepository _repository;

    public EnvironmentService(IEnvironmentRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<Core.Environments.Environment>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public Task<Core.Environments.Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.GetByIdAsync(id, cancellationToken);

    public Task<Core.Environments.Environment> CreateAsync(Core.Environments.Environment environment, CancellationToken cancellationToken = default)
        => _repository.CreateAsync(environment, cancellationToken);

    public Task<Core.Environments.Environment> UpdateAsync(Core.Environments.Environment environment, CancellationToken cancellationToken = default)
        => _repository.UpdateAsync(environment, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => _repository.DeleteAsync(id, cancellationToken);
}
