using BackendTestingStudio.Core.Plugins;

namespace BackendTestingStudio.Application;

public sealed class PluginWorkspace : IPluginWorkspace
{
    private readonly IDeclarativePluginLoader _loader;

    public PluginWorkspace(IDeclarativePluginLoader loader)
    {
        _loader = loader;
    }

    public PluginLoadResult? Active { get; private set; }
    public event EventHandler? Changed;

    public async Task<PluginLoadResult> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        Active = await _loader.LoadAsync(filePath, cancellationToken).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
        return Active;
    }

    public Task<PluginLoadResult?> ReloadAsync(CancellationToken cancellationToken = default)
        => Active is null ? Task.FromResult<PluginLoadResult?>(null) : ReloadCoreAsync(Active.FilePath, cancellationToken);

    private async Task<PluginLoadResult?> ReloadCoreAsync(string path, CancellationToken cancellationToken)
    {
        Active = await _loader.LoadAsync(path, cancellationToken).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
        return Active;
    }
}
