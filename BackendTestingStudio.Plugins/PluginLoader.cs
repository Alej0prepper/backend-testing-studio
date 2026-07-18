using System.Reflection;
using BackendTestingStudio.Core.Plugins;

namespace BackendTestingStudio.Plugins;

public sealed class PluginLoader : IPluginLoader
{
    public Task<IReadOnlyList<PluginDefinition>> GetInstalledAsync(CancellationToken cancellationToken = default)
    {
        var plugins = AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(GetPluginTypes)
            .DistinctBy(type => type.AssemblyQualifiedName)
            .Select(type => Activator.CreateInstance(type) as IPluginModule
                ?? throw new InvalidOperationException($"Could not create plugin '{type.FullName}'."))
            .Select(module => module.Definition)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult<IReadOnlyList<PluginDefinition>>(plugins);
    }

    private static IEnumerable<Type> GetPluginTypes(Assembly assembly)
    {
        var fullName = assembly.FullName;
        if (string.IsNullOrWhiteSpace(fullName) || !fullName.StartsWith("BackendTestingStudio", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        try
        {
            return assembly.GetTypes()
                .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IPluginModule).IsAssignableFrom(type));
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(type => type is not null && !type.IsAbstract && !type.IsInterface && typeof(IPluginModule).IsAssignableFrom(type))
                .Cast<Type>();
        }
    }
}
