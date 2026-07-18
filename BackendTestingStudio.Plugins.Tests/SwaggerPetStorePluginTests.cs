using System.Text.Json;
using Xunit;

namespace BackendTestingStudio.Plugins.Tests;

public sealed class SwaggerPetStorePluginTests
{
    private static readonly string PluginRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "plugins",
        "swagger-petstore"));

    [Fact]
    public void PetStorePlugin_ContainsRequiredContractFiles()
    {
        Assert.True(File.Exists(Path.Combine(PluginRoot, "plugin.json")));
        Assert.True(File.Exists(Path.Combine(PluginRoot, "variables.json")));
        Assert.True(File.Exists(Path.Combine(PluginRoot, "README.md")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "environments")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "modules")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "scenarios")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "payloads")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "assertions")));
    }

    [Fact]
    public void PetStorePlugin_ManifestDefinesPetModuleAndDefaultEnvironment()
    {
        using var manifest = ReadJson("plugin.json");
        var root = manifest.RootElement;

        Assert.Equal("swagger-petstore", root.GetProperty("id").GetString());
        Assert.Equal("Swagger PetStore", root.GetProperty("name").GetString());
        Assert.Equal("swagger-petstore-live", root.GetProperty("defaultEnvironment").GetString());
        Assert.Contains(root.GetProperty("modules").EnumerateArray(), module => module.GetString() == "pet");
    }

    [Fact]
    public void PetStorePlugin_ExposesRequiredCrudEndpoints()
    {
        using var module = ReadJson("modules", "pet.json");
        var endpoints = module.RootElement.GetProperty("endpoints").EnumerateArray().ToArray();

        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "create-pet", "POST", "/pet"));
        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "get-pet", "GET", "/pet/{{PetId}}"));
        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "update-pet", "PUT", "/pet"));
        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "delete-pet", "DELETE", "/pet/{{PetId}}"));
    }

    [Fact]
    public void PetStorePlugin_ScenarioReferencesResolve()
    {
        var endpoints = GetEndpointIds();
        var payloads = GetIds("payloads");
        var assertions = GetIds("assertions");

        foreach (var scenarioPath in Directory.EnumerateFiles(Path.Combine(PluginRoot, "scenarios"), "*.json"))
        {
            using var scenario = JsonDocument.Parse(File.ReadAllText(scenarioPath));
            foreach (var step in scenario.RootElement.GetProperty("steps").EnumerateArray())
            {
                Assert.Contains(step.GetProperty("execute").GetString(), endpoints);

                if (step.TryGetProperty("with", out var stepInput)
                    && stepInput.TryGetProperty("payload", out var payload))
                {
                    Assert.Contains(payload.GetString(), payloads);
                }

                foreach (var assertion in step.GetProperty("assertions").EnumerateArray())
                {
                    Assert.Contains(assertion.GetString(), assertions);
                }
            }
        }
    }

    [Fact]
    public void PetStorePlugin_UsesSwaggerPetStoreBaseUrl()
    {
        using var environment = ReadJson("environments", "swagger-petstore-live.json");

        Assert.Equal("https://petstore.swagger.io/v2", environment.RootElement.GetProperty("baseUrl").GetString());
        Assert.Equal("ApiKey", environment.RootElement.GetProperty("authentication").GetProperty("type").GetString());
    }

    private static bool MatchesEndpoint(JsonElement endpoint, string id, string method, string path)
        => endpoint.GetProperty("id").GetString() == id
            && endpoint.GetProperty("method").GetString() == method
            && endpoint.GetProperty("path").GetString() == path;

    private static HashSet<string?> GetEndpointIds()
    {
        using var module = ReadJson("modules", "pet.json");
        return module.RootElement
            .GetProperty("endpoints")
            .EnumerateArray()
            .Select(endpoint => endpoint.GetProperty("id").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string?> GetIds(string folder)
        => Directory.EnumerateFiles(Path.Combine(PluginRoot, folder), "*.json")
            .Select(path =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return document.RootElement.GetProperty("id").GetString();
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static JsonDocument ReadJson(params string[] paths)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine([PluginRoot, .. paths])));
}

public sealed class DummyJsonPluginTests
{
    private static readonly string PluginRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..",
        "..",
        "..",
        "..",
        "plugins",
        "dummyjson"));

    [Fact]
    public void DummyJsonPlugin_ContainsRequiredContractFiles()
    {
        Assert.True(File.Exists(Path.Combine(PluginRoot, "plugin.json")));
        Assert.True(File.Exists(Path.Combine(PluginRoot, "variables.json")));
        Assert.True(File.Exists(Path.Combine(PluginRoot, "README.md")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "environments")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "modules")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "scenarios")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "payloads")));
        Assert.True(Directory.Exists(Path.Combine(PluginRoot, "assertions")));
    }

    [Fact]
    public void DummyJsonPlugin_ManifestDefinesRequiredModulesAndDefaultEnvironment()
    {
        using var manifest = ReadJson("plugin.json");
        var root = manifest.RootElement;
        var modules = root.GetProperty("modules").EnumerateArray().Select(module => module.GetString()).ToArray();

        Assert.Equal("dummyjson", root.GetProperty("id").GetString());
        Assert.Equal("DummyJSON", root.GetProperty("name").GetString());
        Assert.Equal("dummyjson-live", root.GetProperty("defaultEnvironment").GetString());
        Assert.Contains("auth", modules);
        Assert.Contains("products", modules);
        Assert.Contains("carts", modules);
        Assert.Contains("users", modules);
    }

    [Fact]
    public void DummyJsonPlugin_ExposesRequiredFeatureEndpoints()
    {
        var endpoints = GetEndpoints();

        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "login", "POST", "/auth/login"));
        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "get-auth-user", "GET", "/auth/me"));
        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "list-products", "GET", "/products"));
        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "get-product", "GET", "/products/{{ProductId}}"));
        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "add-cart", "POST", "/carts/add"));
        Assert.Contains(endpoints, endpoint => MatchesEndpoint(endpoint, "get-user", "GET", "/users/{{UserId}}"));
    }

    [Fact]
    public void DummyJsonPlugin_ScenarioReferencesResolve()
    {
        var endpoints = GetEndpointIds();
        var payloads = GetIds("payloads");
        var assertions = GetIds("assertions");

        foreach (var scenarioPath in Directory.EnumerateFiles(Path.Combine(PluginRoot, "scenarios"), "*.json"))
        {
            using var scenario = JsonDocument.Parse(File.ReadAllText(scenarioPath));
            foreach (var step in scenario.RootElement.GetProperty("steps").EnumerateArray())
            {
                Assert.Contains(step.GetProperty("execute").GetString(), endpoints);

                if (step.TryGetProperty("with", out var stepInput)
                    && stepInput.TryGetProperty("payload", out var payload))
                {
                    Assert.Contains(payload.GetString(), payloads);
                }

                foreach (var assertion in step.GetProperty("assertions").EnumerateArray())
                {
                    Assert.Contains(assertion.GetString(), assertions);
                }
            }
        }
    }

    [Fact]
    public void DummyJsonPlugin_UsesDummyJsonBaseUrl()
    {
        using var environment = ReadJson("environments", "dummyjson-live.json");

        Assert.Equal("https://dummyjson.com", environment.RootElement.GetProperty("baseUrl").GetString());
        Assert.Equal("Bearer", environment.RootElement.GetProperty("authentication").GetProperty("type").GetString());
    }

    private static bool MatchesEndpoint(JsonElement endpoint, string id, string method, string path)
        => endpoint.GetProperty("id").GetString() == id
            && endpoint.GetProperty("method").GetString() == method
            && endpoint.GetProperty("path").GetString() == path;

    private static JsonElement[] GetEndpoints()
        => Directory.EnumerateFiles(Path.Combine(PluginRoot, "modules"), "*.json")
            .SelectMany(path =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return document.RootElement
                    .GetProperty("endpoints")
                    .EnumerateArray()
                    .Select(endpoint => endpoint.Clone())
                    .ToArray();
            })
            .ToArray();

    private static HashSet<string?> GetEndpointIds()
        => GetEndpoints()
            .Select(endpoint => endpoint.GetProperty("id").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static HashSet<string?> GetIds(string folder)
        => Directory.EnumerateFiles(Path.Combine(PluginRoot, folder), "*.json")
            .Select(path =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return document.RootElement.GetProperty("id").GetString();
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static JsonDocument ReadJson(params string[] paths)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine([PluginRoot, .. paths])));
}
