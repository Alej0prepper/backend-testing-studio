using BackendTestingStudio.Core.Assertions;
using BackendTestingStudio.Core.Http;

namespace BackendTestingStudio.Core.Scenarios;

public sealed record ScenarioStepDefinition
{
    public ScenarioStepDefinition(
        string name,
        ScenarioHttpMethod method,
        HttpRequestDefinition request,
        IReadOnlyList<AssertionDefinition>? assertions = null,
        IReadOnlyList<ScenarioVariableCapture>? saveVariables = null,
        IReadOnlyDictionary<string, string?>? variables = null,
        bool stopOnFailure = true,
        bool enabled = true,
        string? description = null)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Name is required.", nameof(name))
            : name.Trim();
        Method = method;
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Assertions = assertions ?? [];
        SaveVariables = saveVariables ?? [];
        Variables = variables is null
            ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(variables, StringComparer.OrdinalIgnoreCase);
        StopOnFailure = stopOnFailure;
        Enabled = enabled;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public string Name { get; }

    public ScenarioHttpMethod Method { get; }

    public HttpRequestDefinition Request { get; }

    public IReadOnlyList<AssertionDefinition> Assertions { get; }

    public IReadOnlyList<ScenarioVariableCapture> SaveVariables { get; }

    public IReadOnlyDictionary<string, string?> Variables { get; }

    public bool StopOnFailure { get; }

    public bool Enabled { get; }

    public string? Description { get; }
}
