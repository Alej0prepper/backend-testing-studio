using System.Text.RegularExpressions;

namespace BackendTestingStudio.Core.Payloads;

public sealed record PayloadDefinition
{
    private static readonly Regex TemplatePattern = new(@"\{\{\s*(?<name>[A-Za-z0-9_.-]+)\s*\}\}", RegexOptions.Compiled);

    public PayloadDefinition(
        Guid id,
        string name,
        string description,
        string json,
        IReadOnlyList<PayloadVariable>? variables = null,
        IReadOnlyList<string>? tags = null)
    {
        Id = id;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Description is required.", nameof(description)) : description.Trim();
        Json = json ?? string.Empty;
        Variables = variables ?? [];
        Tags = tags?.Select(tag => tag.Trim()).Where(tag => !string.IsNullOrWhiteSpace(tag)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? [];
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Description { get; }

    public string Json { get; }

    public IReadOnlyList<PayloadVariable> Variables { get; }

    public IReadOnlyList<string> Tags { get; }

    public string RenderJson()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in Variables)
        {
            values[variable.Name] = variable.Value ?? string.Empty;
        }

        return TemplatePattern.Replace(Json, match =>
        {
            var key = match.Groups["name"].Value;
            return values.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
