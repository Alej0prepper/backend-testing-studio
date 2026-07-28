using BackendTestingStudio.Application;
using BackendTestingStudio.Assertions.Assertions;
using BackendTestingStudio.Core.Assertions;
using BackendTestingStudio.Core.Plugins;
using BackendTestingStudio.Core.Reporting;
using BackendTestingStudio.Core.Runs;
using BackendTestingStudio.Core.Scenarios;
using BackendTestingStudio.Http;
using BackendTestingStudio.Plugins;
using BackendTestingStudio.Reporting;
using BackendTestingStudio.Scenarios.Scenarios;
using BackendTestingStudio.Storage;
using Microsoft.Extensions.DependencyInjection;

return await Cli.RunAsync(args);

internal static class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var options = Parse(args.Skip(1).ToArray());
        if (!options.TryGetValue("plugin", out var pluginPath) || string.IsNullOrWhiteSpace(pluginPath))
        {
            Console.Error.WriteLine("Error: --plugin <path/plugin.json> is required.");
            return 2;
        }

        var services = new ServiceCollection();
        services.AddBackendTestingStudioHttp();
        services.AddBackendTestingStudioPlugins();
        services.AddBackendTestingStudioStorage(options.GetValueOrDefault("database"));
        services.AddBackendTestingStudioApplication();
        services.AddSingleton<IAssertionEngine, AssertionEngine>();
        services.AddScoped<IScenarioEngine, ScenarioEngine>();
        services.AddSingleton<IReportEngine, ReportEngine>();
        await using var provider = services.BuildServiceProvider();
        var loader = provider.GetRequiredService<IDeclarativePluginLoader>();

        if (command == "validate")
        {
            var result = await loader.LoadAsync(pluginPath);
            PrintDiagnostics(result);
            return result.IsValid ? 0 : 2;
        }

        var load = await loader.LoadAsync(pluginPath);
        if (!load.IsValid || load.Plugin is null)
        {
            PrintDiagnostics(load);
            return 2;
        }

        if (command == "list")
        {
            Console.WriteLine($"Plugin: {load.Plugin.Name} {load.Plugin.Version}");
            Console.WriteLine("Environments:");
            foreach (var environment in load.Plugin.Environments)
            {
                Console.WriteLine($"  {environment.Id} ({environment.Level}) -> {environment.BaseUrl}");
            }

            Console.WriteLine("Scenarios:");
            foreach (var scenario in load.Plugin.Scenarios)
            {
                Console.WriteLine($"  {scenario.Id} [{string.Join(",", scenario.Tags)}] - {scenario.Name}");
            }

            return 0;
        }

        if (command != "run")
        {
            Console.Error.WriteLine($"Unknown command '{command}'.");
            PrintHelp();
            return 2;
        }

        if (!options.TryGetValue("scenario", out var scenarioId) || string.IsNullOrWhiteSpace(scenarioId))
        {
            Console.Error.WriteLine("Error: run requires --scenario <id>.");
            return 2;
        }

        var environmentId = options.GetValueOrDefault("environment") ?? load.Plugin.DefaultEnvironment;
        var overrides = ParseVariables(args);
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        var runService = provider.GetRequiredService<IScenarioRunService>();
        var resultRun = await runService.RunAsync(
            new ScenarioRunRequest(
                new DeclarativeRunPlugin(pluginPath, load.Plugin.Id, load.Plugin.Version),
                scenarioId,
                environmentId,
                overrides,
                options.ContainsKey("allow-production"),
                ParseTimeout(options.GetValueOrDefault("timeout"))),
            cancellation.Token);
        if (resultRun.Report is not null)
        {
            PrintSummary(resultRun.Report, resultRun.RunId);
            var reportEngine = provider.GetRequiredService<IReportEngine>();
            await WriteReportAsync(options, "json", ReportExportFormat.Json, resultRun.Report, reportEngine);
            await WriteReportAsync(options, "html", ReportExportFormat.Html, resultRun.Report, reportEngine);
            await WriteReportAsync(options, "junit", ReportExportFormat.JUnit, resultRun.Report, reportEngine);
        }
        else
        {
            Console.Error.WriteLine(resultRun.Error);
        }

        return resultRun.FailureKind switch
        {
            ScenarioRunFailureKind.None when resultRun.Passed => 0,
            ScenarioRunFailureKind.None => 1,
            ScenarioRunFailureKind.Validation or ScenarioRunFailureKind.Configuration or
                ScenarioRunFailureKind.ProductionGuard => 2,
            _ => 3
        };
    }

    private static Dictionary<string, string?> Parse(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var name = args[index][2..];
            if (name == "var")
            {
                index++;
                continue;
            }

            result[name] = index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++index]
                : null;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, string?> ParseVariables(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index] != "--var")
            {
                continue;
            }

            var pair = args[++index].Split('=', 2);
            if (pair.Length != 2 || string.IsNullOrWhiteSpace(pair[0]))
            {
                throw new ArgumentException("--var values must use Name=Value.");
            }

            result[pair[0]] = pair[1];
        }

        return result;
    }

    private static TimeSpan? ParseTimeout(string? value)
        => int.TryParse(value, out var milliseconds) && milliseconds > 0
            ? TimeSpan.FromMilliseconds(milliseconds)
            : null;

    private static async Task WriteReportAsync(
        IReadOnlyDictionary<string, string?> options,
        string option,
        ReportExportFormat format,
        ExecutionReport report,
        IReportEngine engine)
    {
        var path = options.GetValueOrDefault(option);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, engine.Export(report, format));
        Console.WriteLine($"Report: {fullPath}");
    }

    private static void PrintDiagnostics(PluginLoadResult result)
    {
        Console.WriteLine(result.IsValid ? $"Valid: {result.FilePath}" : $"Invalid: {result.FilePath}");
        foreach (var item in result.Diagnostics)
        {
            Console.WriteLine($"{item.Severity}: {item.JsonPath} [{item.Rule}] {item.Message}");
        }
    }

    private static void PrintSummary(ExecutionReport report, Guid runId)
    {
        Console.WriteLine($"Run: {runId:N}");
        Console.WriteLine($"Scenario: {report.ScenarioName} ({report.ScenarioId})");
        Console.WriteLine($"Status: {report.Summary.Status}");
        Console.WriteLine($"Steps: {report.Summary.SucceededSteps} passed, {report.Summary.FailedSteps} failed");
        Console.WriteLine($"Assertions: {report.Summary.PassedAssertions} passed, {report.Summary.FailedAssertions} failed");
    }

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            Backend Testing Studio CLI

              bts validate --plugin path/plugin.json
              bts list     --plugin path/plugin.json
              bts run      --plugin path/plugin.json --scenario id [options]

            Run options:
              --environment id
              --var Name=Value              Repeatable; highest-precedence non-secret override
              --timeout milliseconds
              --allow-production            Authorize mutating scenarios in Production
              --json path --html path --junit path

            Secrets are read from BTS_SECRET_<PLUGIN>_<NAME> or BTS_SECRET_<NAME>.
            Exit codes: 0 passed, 1 assertions failed, 2 invalid config/guard, 3 execution error.
            """);
    }
}
