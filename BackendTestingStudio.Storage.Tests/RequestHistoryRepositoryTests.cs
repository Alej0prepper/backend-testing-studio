using BackendTestingStudio.Core.History;
using BackendTestingStudio.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BackendTestingStudio.Storage.Tests;

public sealed class RequestHistoryRepositoryTests
{
    [Fact]
    public async Task CrudRoundtrip_PersistsRequestAndResponseSnapshot()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"bts-history-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();

        services.AddBackendTestingStudioStorage(databasePath);

        using var provider = services.BuildServiceProvider();
        var historyRepository = provider.GetRequiredService<IRequestHistoryRepository>();

        var entry = new RequestHistoryEntry(
            Guid.Empty,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            "Local",
            new RequestHistoryRequestSnapshot(
                "POST",
                "https://localhost:5001/api/items?search=test",
                new Dictionary<string, string?> { ["X-Test"] = "true" },
                RequestHistoryBodyKind.Json,
                """{"name":"item"}"""),
            new RequestHistoryResponseSnapshot(
                System.Net.HttpStatusCode.Created,
                new Dictionary<string, IReadOnlyList<string>>
                {
                    ["content-type"] = ["application/json"]
                },
                """{"id":1}"""),
            42.5);

        var created = await historyRepository.AddAsync(entry);
        var all = await historyRepository.GetAllAsync();

        Assert.Single(all);
        Assert.Equal(created.Id, all[0].Id);
        Assert.Equal("POST", all[0].Method);
        Assert.Equal("https://localhost:5001/api/items?search=test", all[0].Url);
        Assert.Equal("Local", all[0].EnvironmentName);
        Assert.Equal("true", all[0].Request.Headers["X-Test"]);
        Assert.Equal(RequestHistoryBodyKind.Json, all[0].Request.BodyKind);
        Assert.Equal("""{"name":"item"}""", all[0].Request.JsonBody);
        Assert.Equal(System.Net.HttpStatusCode.Created, all[0].Response.StatusCode);
        Assert.Equal("""{"id":1}""", all[0].Response.Body);

        var loaded = await historyRepository.GetByIdAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded!.Id);

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
