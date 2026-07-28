namespace BackendTestingStudio.Core.Security;

public interface ISecretStore
{
    ValueTask<string?> GetAsync(string pluginId, string name, CancellationToken cancellationToken = default);
    ValueTask SetSessionAsync(string pluginId, string name, string value, CancellationToken cancellationToken = default);
    ValueTask ClearAsync(string pluginId, CancellationToken cancellationToken = default);
}

public interface ISensitiveDataRedactor
{
    string? RedactText(string? value, IReadOnlyDictionary<string, string?> secrets);
    IReadOnlyDictionary<string, string?> RedactHeaders(
        IReadOnlyDictionary<string, string?> headers,
        IReadOnlyDictionary<string, string?> secrets);
}
