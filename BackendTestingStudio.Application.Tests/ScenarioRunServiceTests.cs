using System.Net;
using BackendTestingStudio.Application;
using BackendTestingStudio.Assertions.Assertions;
using BackendTestingStudio.Core.Http;
using BackendTestingStudio.Core.Plugins;
using BackendTestingStudio.Core.Runs;
using BackendTestingStudio.Plugins;
using BackendTestingStudio.Reporting;
using BackendTestingStudio.Scenarios.Scenarios;
using Xunit;

namespace BackendTestingStudio.Application.Tests;

public sealed class ScenarioRunServiceTests
{
    [Fact]
    public async Task RunAsync_ExecutesFullPluginAndRedactsInputAndCapturedSecrets()
    {
        var pluginPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "plugins", "dummyjson", "plugin.json"));
        var secretStore = new SessionSecretStore();
        await secretStore.SetSessionAsync("dummyjson", "Password", "top-secret-password");
        var repository = new InMemoryRunRepository();
        var engine = new ScenarioEngine(new StubHttpEngine(), new AssertionEngine());
        var service = new ScenarioRunService(
            new DeclarativePluginLoader(),
            new PluginCompiler(),
            engine,
            new ReportEngine(),
            secretStore,
            new SensitiveDataRedactor(),
            repository);

        var result = await service.RunAsync(new ScenarioRunRequest(
            new DeclarativeRunPlugin(pluginPath, "dummyjson", "1.0.0"),
            "login-and-auth-user",
            "dummyjson-live"));

        Assert.True(result.Passed, result.Error);
        Assert.NotNull(result.Report);
        Assert.Equal(2, result.Report.Steps.Count);
        Assert.Contains(result.Report.Variables, item => item.Name == "AccessToken" && item.Value == "[REDACTED]");
        var persisted = Assert.Single(repository.Runs);
        Assert.DoesNotContain("top-secret-password", persisted.ReportJson);
        Assert.DoesNotContain("captured-access-token", persisted.ReportJson);
        Assert.Contains("[REDACTED]", persisted.ReportJson);
    }

    [Fact]
    public async Task RunAsync_BlocksProductionMutationWithoutExplicitAuthorization()
    {
        var source = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "plugins", "swagger-petstore", "plugin.json"));
        var directory = Path.Combine(Path.GetTempPath(), "bts-production-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "plugin.json");
        var json = (await File.ReadAllTextAsync(source)).Replace(
            "\"level\": \"Development\"",
            "\"level\": \"Production\"",
            StringComparison.Ordinal);
        await File.WriteAllTextAsync(path, json);
        var service = new ScenarioRunService(
            new DeclarativePluginLoader(),
            new PluginCompiler(),
            new ScenarioEngine(new StubHttpEngine(), new AssertionEngine()),
            new ReportEngine(),
            new SessionSecretStore(),
            new SensitiveDataRedactor(),
            new InMemoryRunRepository());

        var result = await service.RunAsync(new ScenarioRunRequest(
            new DeclarativeRunPlugin(path, "swagger-petstore", "1.0.0"),
            "pet-crud-lifecycle",
            "swagger-petstore-live"));

        Assert.Equal(ScenarioRunFailureKind.ProductionGuard, result.FailureKind);
    }

    private sealed class StubHttpEngine : IHttpEngine
    {
        public Task<HttpResponseResult> GetAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => Response("""{"id":1,"username":"emilys"}""", HttpStatusCode.OK);
        public Task<HttpResponseResult> PostAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => Response("""{"id":1,"username":"emilys","accessToken":"captured-access-token"}""", HttpStatusCode.OK);
        public Task<HttpResponseResult> PutAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => Response("{}", HttpStatusCode.OK);
        public Task<HttpResponseResult> PatchAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => Response("{}", HttpStatusCode.OK);
        public Task<HttpResponseResult> DeleteAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => Response("{}", HttpStatusCode.OK);

        private static Task<HttpResponseResult> Response(string body, HttpStatusCode code)
            => Task.FromResult(new HttpResponseResult(
                code,
                body,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)));
    }

    private sealed class InMemoryRunRepository : IScenarioRunRepository
    {
        public List<StoredScenarioRun> Runs { get; } = [];
        public Task AddAsync(StoredScenarioRun run, CancellationToken cancellationToken = default)
        {
            Runs.Add(run);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<StoredScenarioRun>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<StoredScenarioRun>>(Runs);
        public Task<StoredScenarioRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Runs.FirstOrDefault(item => item.Id == id));
        public Task DeleteOlderThanAsync(DateTimeOffset cutoff, int keepLatest, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
