using BackendTestingStudio.Plugins;
using Xunit;

namespace BackendTestingStudio.Plugins.Tests;

public sealed class DeclarativePluginTests
{
    public static IEnumerable<object[]> CanonicalPlugins()
    {
        yield return ["dummyjson"];
        yield return ["swagger-petstore"];
    }

    [Theory]
    [MemberData(nameof(CanonicalPlugins))]
    public async Task CanonicalPlugin_IsSingleFileAndPassesRealLoader(string pluginFolder)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "plugins", pluginFolder, "plugin.json"));

        var result = await new DeclarativePluginLoader().LoadAsync(path);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.NotNull(result.Plugin);
        Assert.NotEmpty(result.Plugin.Environments);
        Assert.NotEmpty(result.Plugin.Modules);
        Assert.NotEmpty(result.Plugin.Scenarios);
    }

    [Fact]
    public async Task MissingAssertion_ReturnsJsonPathAndRule()
    {
        var path = WritePlugin("\"assertions\": [\"does-not-exist\"]");

        var result = await new DeclarativePluginLoader().LoadAsync(path);

        Assert.False(result.IsValid);
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Rule == "assertion.missing");
        Assert.Contains("$.modules", diagnostic.JsonPath);
    }

    [Fact]
    public async Task SensitiveDefault_IsRejected()
    {
        var path = WritePlugin(
            "\"assertions\": []",
            """
            "variables": [{ "name": "Password", "sensitive": true, "defaultValue": "plain" }],
            """);

        var result = await new DeclarativePluginLoader().LoadAsync(path);

        Assert.Contains(result.Diagnostics, item => item.Rule == "secret.inline");
    }

    private static string WritePlugin(string endpointAssertions, string variables = "\"variables\": [],")
    {
        var directory = Path.Combine(Path.GetTempPath(), "bts-plugin-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "plugin.json");
        File.WriteAllText(path,
            $$"""
            {
              "id": "fixture",
              "name": "Fixture",
              "version": "1.0.0",
              "schemaVersion": "1.0.0",
              "engineVersion": "1.0.0",
              "author": "Tests",
              "description": "Fixture plugin",
              "defaultEnvironment": "test",
              {{variables}}
              "environments": [{
                "id": "test", "name": "Test", "baseUrl": "http://localhost:1234",
                "allowedHosts": ["localhost"]
              }],
              "modules": [{
                "id": "health", "name": "Health",
                "endpoints": [{ "id": "get-health", "name": "Get health", "method": "GET", "path": "/health", {{endpointAssertions}} }]
              }],
              "payloads": [],
              "assertions": [],
              "scenarios": [{
                "id": "smoke", "name": "Smoke",
                "steps": [{ "execute": "get-health" }]
              }]
            }
            """);
        return path;
    }
}
