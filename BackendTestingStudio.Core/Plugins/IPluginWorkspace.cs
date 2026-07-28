namespace BackendTestingStudio.Core.Plugins;

public interface IPluginWorkspace
{
    PluginLoadResult? Active { get; }
    event EventHandler? Changed;
    Task<PluginLoadResult> OpenAsync(string filePath, CancellationToken cancellationToken = default);
    Task<PluginLoadResult?> ReloadAsync(CancellationToken cancellationToken = default);
}
