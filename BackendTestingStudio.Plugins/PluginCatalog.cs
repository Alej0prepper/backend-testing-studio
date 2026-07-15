using BackendTestingStudio.Core.Plugins;

namespace BackendTestingStudio.Plugins;

public sealed class BuiltInScenarioPlugin : IPluginModule
{
    public PluginDefinition Definition { get; } = new(
        name: "Built-in Scenarios",
        version: new Version(1, 0, 0),
        author: "Backend Testing Studio",
        description: "Colección inicial de escenarios y ejemplos para validar el sistema de plugins.",
        endpoints:
        [
            new PluginEndpointDefinition("Ping", "GET", "/ping", "Verifica conectividad básica.")
        ],
        scenarios:
        [
            new PluginScenarioDefinition(
                "Healthcheck",
                "Ejecuta un request simple para validar el entorno.",
                [
                    new PluginScenarioStepDefinition("Ping service", "Ping")
                ])
        ],
        payloads:
        [
            new PluginPayloadDefinition("Empty", "Payload vacío para requests que no requieren body.")
        ],
        variables:
        [
            new PluginVariableDefinition("basePath", "/api", "Ruta base reutilizable por los escenarios.")
        ]);
}

