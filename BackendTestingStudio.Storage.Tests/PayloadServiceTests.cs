using BackendTestingStudio.Core.Payloads;
using BackendTestingStudio.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BackendTestingStudio.Storage.Tests;

public sealed class PayloadServiceTests
{
    [Fact]
    public async Task CrudRoundtrip_PersistsPayloadVariablesAndTags()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"bts-payloads-{Guid.NewGuid():N}.db");
        var services = new ServiceCollection();

        services.AddBackendTestingStudioStorage(databasePath);

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<IPayloadService>();

        var created = await service.CreateAsync(
            new PayloadDefinition(
                Guid.Empty,
                "Create order",
                "Reusable order payload",
                """
                {
                  "customerId": "{{CustomerId}}",
                  "items": [
                    {
                      "sku": "{{Sku}}",
                      "quantity": {{Quantity}}
                    }
                  ]
                }
                """,
                [
                    new PayloadVariable(Guid.Empty, "CustomerId", "42"),
                    new PayloadVariable(Guid.Empty, "Sku", "ABC-1"),
                    new PayloadVariable(Guid.Empty, "Quantity", "3")
                ],
                ["orders", "api"]));

        var all = await service.GetAllAsync();
        Assert.Single(all);
        Assert.Equal("Create order", all[0].Name);
        Assert.Equal("Reusable order payload", all[0].Description);
        Assert.Equal(3, all[0].Variables.Count);
        Assert.Equal(2, all[0].Tags.Count);
        Assert.Equal("42", all[0].Variables[0].Value);
        Assert.Equal("orders", all[0].Tags[0]);

        var loaded = await service.GetByIdAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.Equal(created.Id, loaded!.Id);
        Assert.Equal("ABC-1", loaded.Variables[1].Value);
        Assert.Contains("42", loaded.RenderJson());
        Assert.DoesNotContain("{{", loaded.RenderJson());

        var updated = await service.UpdateAsync(
            new PayloadDefinition(
                created.Id,
                "Create order updated",
                "Reusable order payload",
                created.Json,
                created.Variables,
                ["orders"]));

        Assert.Equal("Create order updated", updated.Name);

        await service.DeleteAsync(created.Id);
        Assert.Empty(await service.GetAllAsync());

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public void RenderJson_ReplacesRepeatedVariables()
    {
        var payload = new PayloadDefinition(
            Guid.NewGuid(),
            "Demo",
            "Demo payload",
            """
            {
              "token": "{{Token}}",
              "nested": {
                "again": "{{Token}}"
              }
            }
            """,
            [new PayloadVariable(Guid.NewGuid(), "Token", "abc-123")]);

        var rendered = payload.RenderJson();

        Assert.Contains("abc-123", rendered);
        Assert.DoesNotContain("{{Token}}", rendered);
    }
}
