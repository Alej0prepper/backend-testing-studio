using BackendTestingStudio.Core.Plugins;
using BackendTestingStudio.Core.Runs;
using BackendTestingStudio.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace BackendTestingStudio.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendTestingStudioApplication(
        this IServiceCollection services,
        Action<ScenarioRunOptions>? configure = null)
    {
        var options = new ScenarioRunOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<IPluginWorkspace, PluginWorkspace>();
        services.AddSingleton<ISecretStore, SessionSecretStore>();
        services.AddSingleton<ISensitiveDataRedactor, SensitiveDataRedactor>();
        services.AddScoped<IScenarioRunService, ScenarioRunService>();
        return services;
    }
}
