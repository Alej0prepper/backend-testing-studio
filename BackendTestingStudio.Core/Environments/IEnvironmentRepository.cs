namespace BackendTestingStudio.Core.Environments;

public interface IEnvironmentRepository
{
    Task<IReadOnlyList<Environment>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Environment> CreateAsync(Environment environment, CancellationToken cancellationToken = default);

    Task<Environment> UpdateAsync(Environment environment, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
