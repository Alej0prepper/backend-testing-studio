using BackendTestingStudio.Core.Environments;
using Microsoft.Extensions.DependencyInjection;
using SQLitePCL;

namespace BackendTestingStudio.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendTestingStudioStorage(this IServiceCollection services, string? databasePath = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        Batteries_V2.Init();

        services.AddSingleton(new Environments.EnvironmentStorageOptions
        {
            DatabasePath = databasePath ?? Path.Combine(AppContext.BaseDirectory, "backend-testing-studio.environments.db")
        });
        services.AddSingleton<IEnvironmentRepository, Environments.EnvironmentRepository>();
        services.AddSingleton<IEnvironmentService, Environments.EnvironmentService>();

        return services;
    }
}
