using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using BackendTestingStudio.Core.Security;

namespace BackendTestingStudio.Application;

public sealed class SessionSecretStore : ISecretStore
{
    private readonly ConcurrentDictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public ValueTask<string?> GetAsync(string pluginId, string name, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_values.TryGetValue(Key(pluginId, name), out var value))
        {
            return ValueTask.FromResult<string?>(value);
        }

        var scoped = Environment.GetEnvironmentVariable(ToEnvironmentName(pluginId, name));
        var unscoped = Environment.GetEnvironmentVariable(ToEnvironmentName(null, name));
        return ValueTask.FromResult<string?>(scoped ?? unscoped);
    }

    public ValueTask SetSessionAsync(
        string pluginId,
        string name,
        string value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _values[Key(pluginId, name)] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var key in _values.Keys.Where(key => key.StartsWith(pluginId + ":", StringComparison.OrdinalIgnoreCase)))
        {
            _values.TryRemove(key, out _);
        }

        return ValueTask.CompletedTask;
    }

    public static string ToEnvironmentName(string? pluginId, string name)
    {
        var raw = pluginId is null ? $"BTS_SECRET_{name}" : $"BTS_SECRET_{pluginId}_{name}";
        return Regex.Replace(raw.ToUpperInvariant(), "[^A-Z0-9_]", "_");
    }

    private static string Key(string pluginId, string name) => $"{pluginId}:{name}";
}

public sealed class SensitiveDataRedactor : ISensitiveDataRedactor
{
    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie", "X-Api-Key", "Api-Key"
    };

    public string? RedactText(string? value, IReadOnlyDictionary<string, string?> secrets)
    {
        if (value is null)
        {
            return null;
        }

        var result = value;
        foreach (var secret in secrets.Values
                     .Where(item => !string.IsNullOrEmpty(item))
                     .Distinct(StringComparer.Ordinal)
                     .OrderByDescending(item => item!.Length))
        {
            result = result.Replace(secret!, "[REDACTED]", StringComparison.Ordinal);
        }

        return result;
    }

    public IReadOnlyDictionary<string, string?> RedactHeaders(
        IReadOnlyDictionary<string, string?> headers,
        IReadOnlyDictionary<string, string?> secrets)
        => headers.ToDictionary(
            item => item.Key,
            item => SensitiveHeaders.Contains(item.Key) ? "[REDACTED]" : RedactText(item.Value, secrets),
            StringComparer.OrdinalIgnoreCase);
}
