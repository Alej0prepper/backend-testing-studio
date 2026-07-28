using BackendTestingStudio.Core.Environments;
using System.Collections.Concurrent;

namespace BackendTestingStudio.Storage.Environments;

internal sealed class EnvironmentService : IEnvironmentService
{
    private readonly IEnvironmentRepository _repository;
    private readonly ConcurrentDictionary<Guid, EnvironmentAuthentication?> _sessionAuthentication = [];

    public EnvironmentService(IEnvironmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Core.Environments.Environment>> GetAllAsync(CancellationToken cancellationToken = default)
        => (await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false)).Select(WithSessionAuthentication).ToArray();

    public async Task<Core.Environments.Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var environment = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return environment is null ? null : WithSessionAuthentication(environment);
    }

    public async Task<Core.Environments.Environment> CreateAsync(Core.Environments.Environment environment, CancellationToken cancellationToken = default)
    {
        var saved = await _repository.CreateAsync(environment, cancellationToken).ConfigureAwait(false);
        StoreSessionAuthentication(saved.Id, environment.Authentication);
        return WithSessionAuthentication(saved);
    }

    public async Task<Core.Environments.Environment> UpdateAsync(Core.Environments.Environment environment, CancellationToken cancellationToken = default)
    {
        var saved = await _repository.UpdateAsync(environment, cancellationToken).ConfigureAwait(false);
        StoreSessionAuthentication(saved.Id, environment.Authentication);
        return WithSessionAuthentication(saved);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        _sessionAuthentication.TryRemove(id, out _);
    }

    private Core.Environments.Environment WithSessionAuthentication(Core.Environments.Environment environment)
        => _sessionAuthentication.TryGetValue(environment.Id, out var authentication)
            ? new Core.Environments.Environment(
                environment.Id,
                environment.Name,
                environment.BaseUrl,
                environment.Variables,
                environment.Headers,
                authentication)
            : environment;

    private void StoreSessionAuthentication(Guid id, EnvironmentAuthentication? authentication)
    {
        if (authentication is null)
        {
            _sessionAuthentication.TryRemove(id, out _);
        }
        else
        {
            _sessionAuthentication[id] = authentication;
        }
    }
}
