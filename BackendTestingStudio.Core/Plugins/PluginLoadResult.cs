namespace BackendTestingStudio.Core.Plugins;

public enum PluginDiagnosticSeverity
{
    Warning,
    Error
}

public sealed record PluginDiagnostic(
    PluginDiagnosticSeverity Severity,
    string File,
    string JsonPath,
    string Rule,
    string Message);

public sealed record PluginLoadResult(
    string FilePath,
    DeclarativePlugin? Plugin,
    IReadOnlyList<PluginDiagnostic> Diagnostics)
{
    public bool IsValid => Plugin is not null && Diagnostics.All(item => item.Severity != PluginDiagnosticSeverity.Error);
}

public interface IDeclarativePluginLoader
{
    Task<PluginLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default);
}
