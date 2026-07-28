using BackendTestingStudio.Core.Scenarios;

namespace BackendTestingStudio.Core.Plugins;

public interface IPluginCompiler
{
    ScenarioDefinition Compile(
        DeclarativePlugin plugin,
        string scenarioId,
        string environmentId,
        IReadOnlyDictionary<string, string?>? variables = null);
}
