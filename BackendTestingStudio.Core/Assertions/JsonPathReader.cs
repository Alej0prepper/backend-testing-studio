using System.Text.Json;

namespace BackendTestingStudio.Core.Assertions;

public static class JsonPathReader
{
    public static IReadOnlyList<string> ReadValues(string json, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var document = JsonDocument.Parse(json);
        return Select(document.RootElement, path).Select(Format).ToArray();
    }

    private static IReadOnlyList<JsonElement> Select(JsonElement root, string path)
    {
        if (path[0] != '$')
        {
            throw new ArgumentException("JSONPath must start with '$'.", nameof(path));
        }

        var current = new List<JsonElement> { root };
        var index = 1;

        while (index < path.Length)
        {
            switch (path[index])
            {
                case '.':
                    index++;
                    var property = ReadIdentifier(path, ref index);
                    current = current.SelectMany(element => SelectProperty(element, property)).ToList();
                    break;
                case '[':
                    var token = ReadBracketToken(path, ref index);
                    current = current.SelectMany(element => SelectBracket(element, token)).ToList();
                    break;
                default:
                    throw new ArgumentException($"Unexpected token at position {index} in JSONPath '{path}'.", nameof(path));
            }
        }

        return current;
    }

    private static IEnumerable<JsonElement> SelectProperty(JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var child) ? [child] : [];

    private static IEnumerable<JsonElement> SelectBracket(JsonElement element, string token)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        if (token == "*")
        {
            return element.EnumerateArray().ToArray();
        }

        return int.TryParse(token, out var index) && index >= 0 && index < element.GetArrayLength()
            ? [element[index]]
            : [];
    }

    private static string ReadIdentifier(string path, ref int index)
    {
        var start = index;
        while (index < path.Length && (char.IsLetterOrDigit(path[index]) || path[index] is '_' or '-'))
        {
            index++;
        }

        if (index == start)
        {
            throw new ArgumentException($"Missing property name in JSONPath '{path}'.", nameof(path));
        }

        return path[start..index];
    }

    private static string ReadBracketToken(string path, ref int index)
    {
        index++;
        var start = index;
        while (index < path.Length && path[index] != ']')
        {
            index++;
        }

        if (index >= path.Length)
        {
            throw new ArgumentException($"Unclosed bracket in JSONPath '{path}'.", nameof(path));
        }

        var token = path[start..index].Trim();
        index++;
        return token;
    }

    private static string Format(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Null => "null",
            JsonValueKind.Object or JsonValueKind.Array => element.GetRawText(),
            _ => element.ToString()
        };
}
