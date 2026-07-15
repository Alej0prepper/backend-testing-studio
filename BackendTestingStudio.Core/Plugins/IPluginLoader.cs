namespace BackendTestingStudio.Core.Plugins;

public interface IPluginLoader
{
    Task<IReadOnlyList<PluginDefinition>> GetInstalledAsync(CancellationToken cancellationToken = default);
}
