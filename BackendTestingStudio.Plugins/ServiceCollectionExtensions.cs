using BackendTestingStudio.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace BackendTestingStudio.Plugins;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendTestingStudioPlugins(this IServiceCollection services)
    {
        services.AddSingleton<IDeclarativePluginLoader, DeclarativePluginLoader>();
        services.AddSingleton<IPluginCompiler, PluginCompiler>();
        return services;
    }
}
