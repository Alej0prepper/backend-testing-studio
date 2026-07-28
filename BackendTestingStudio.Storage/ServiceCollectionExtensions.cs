using BackendTestingStudio.Core.Environments;
using BackendTestingStudio.Core.History;
using BackendTestingStudio.Core.Payloads;
using BackendTestingStudio.Core.Runs;
using Microsoft.Extensions.DependencyInjection;
using SQLitePCL;

namespace BackendTestingStudio.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendTestingStudioStorage(
        this IServiceCollection services,
        string? databasePath = null,
        int maximumReportBytes = 2 * 1024 * 1024)
    {
        ArgumentNullException.ThrowIfNull(services);

        Batteries_V2.Init();

        var resolvedPath = databasePath ?? Path.Combine(AppContext.BaseDirectory, "backend-testing-studio.environments.db");
        services.AddSingleton(new Environments.EnvironmentStorageOptions
        {
            DatabasePath = resolvedPath
        });
        services.AddSingleton(new Runs.ScenarioRunStorageOptions
        {
            DatabasePath = resolvedPath,
            MaximumReportBytes = Math.Max(1024, maximumReportBytes)
        });
        services.AddSingleton<IEnvironmentRepository, Environments.EnvironmentRepository>();
        services.AddSingleton<IEnvironmentService, Environments.EnvironmentService>();
        services.AddSingleton<IRequestHistoryRepository, History.RequestHistoryRepository>();
        services.AddSingleton<IPayloadRepository, Payloads.PayloadRepository>();
        services.AddSingleton<IPayloadService, Payloads.PayloadService>();
        services.AddSingleton<IScenarioRunRepository, Runs.ScenarioRunRepository>();

        return services;
    }
}
