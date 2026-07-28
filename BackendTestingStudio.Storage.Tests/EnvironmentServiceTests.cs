using EnvironmentEntity = BackendTestingStudio.Core.Environments.Environment;
using EnvironmentVariableEntity = BackendTestingStudio.Core.Environments.EnvironmentVariable;
using EnvironmentAuthenticationBearer = BackendTestingStudio.Core.Environments.EnvironmentAuthenticationBearer;
using EnvironmentAuthenticationBasic = BackendTestingStudio.Core.Environments.EnvironmentAuthenticationBasic;
using IEnvironmentService = BackendTestingStudio.Core.Environments.IEnvironmentService;
using BackendTestingStudio.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BackendTestingStudio.Storage.Tests;

public sealed class EnvironmentServiceTests
{
    [Fact]
    public async Task CrudRoundtrip_PersistsEnvironmentsVariablesAndHeaders()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"bts-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();

        services.AddBackendTestingStudioStorage(databasePath);

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IEnvironmentService>();

        var created = await service.CreateAsync(
            new EnvironmentEntity(
                Guid.Empty,
                "Local",
                "https://localhost:5001",
                [
                    new EnvironmentVariableEntity(Guid.Empty, "ApiUrl", "https://localhost:5001/api")
                ],
                [
                    new EnvironmentVariableEntity(Guid.Empty, "X-Debug", "true")
                ],
                new EnvironmentAuthenticationBearer("token-123")));

        var all = await service.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Local", all[0].Name);
        Assert.Equal("https://localhost:5001", all[0].BaseUrl);
        Assert.Single(all[0].Variables);
        Assert.Single(all[0].Headers);
        Assert.IsType<EnvironmentAuthenticationBearer>(all[0].Authentication);
        Assert.Equal("token-123", ((EnvironmentAuthenticationBearer)all[0].Authentication!).Token);

        var loaded = await service.GetByIdAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal("ApiUrl", loaded!.Variables[0].Name);
        Assert.Equal("X-Debug", loaded.Headers[0].Name);

        var updated = await service.UpdateAsync(
            new EnvironmentEntity(
                created.Id,
                "Updated",
                "https://localhost:6001",
                [
                    new EnvironmentVariableEntity(loaded.Variables[0].Id, "ApiUrl", "https://localhost:6001/api")
                ],
                [],
                new EnvironmentAuthenticationBasic("demo", "secret")));

        Assert.Equal("Updated", updated.Name);

        var afterUpdate = await service.GetByIdAsync(created.Id);
        Assert.NotNull(afterUpdate);
        Assert.Equal("https://localhost:6001", afterUpdate!.BaseUrl);
        Assert.Single(afterUpdate.Variables);
        Assert.Empty(afterUpdate.Headers);
        Assert.IsType<EnvironmentAuthenticationBasic>(afterUpdate.Authentication);

        await service.DeleteAsync(created.Id);
        Assert.Empty(await service.GetAllAsync());

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task SensitiveNamedVariable_IsRejectedBeforePersistence()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"bts-secret-{Guid.NewGuid():N}.db");
        try
        {
            var services = new ServiceCollection();
            services.AddBackendTestingStudioStorage(databasePath);
            await using var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IEnvironmentService>();

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CreateAsync(new EnvironmentEntity(
                    Guid.Empty,
                    "Unsafe",
                    "https://localhost:5001",
                    [new EnvironmentVariableEntity(Guid.Empty, "AccessToken", "must-not-persist")],
                    [])));

            Assert.Contains("session secret store", error.Message);
            Assert.DoesNotContain("must-not-persist", System.Text.Encoding.UTF8.GetString(await File.ReadAllBytesAsync(databasePath)));
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
