using BackendTestingStudio.Core.Runs;
using BackendTestingStudio.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BackendTestingStudio.Storage.Tests;

public sealed class ScenarioRunRepositoryTests
{
    [Fact]
    public async Task RoundtripAndRetention_PersistSanitizedRunShape()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"bts-runs-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddBackendTestingStudioStorage(databasePath);
            await using var provider = services.BuildServiceProvider();
            var repository = provider.GetRequiredService<IScenarioRunRepository>();
            var old = Run(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(-40), "old");
            var current = Run(Guid.NewGuid(), DateTimeOffset.UtcNow, "current");

            await repository.AddAsync(old);
            await repository.AddAsync(current);
            Assert.Equal(2, (await repository.GetAllAsync()).Count);

            await repository.DeleteOlderThanAsync(DateTimeOffset.UtcNow.AddDays(-30), 200);

            var remaining = Assert.Single(await repository.GetAllAsync());
            Assert.Equal(current.Id, remaining.Id);
            Assert.Equal("""{"scenarioId":"smoke"}""", remaining.ReportJson);
            Assert.NotNull(await repository.GetByIdAsync(current.Id));
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static StoredScenarioRun Run(Guid id, DateTimeOffset createdAt, string status)
        => new(
            id,
            createdAt,
            "fixture",
            "1.0.0",
            "smoke",
            "Smoke",
            "test",
            status,
            id.ToString("N"),
            "{}",
            "{}",
            """{"scenarioId":"smoke"}""");
}
