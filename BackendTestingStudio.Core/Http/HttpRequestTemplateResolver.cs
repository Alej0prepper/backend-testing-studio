using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace BackendTestingStudio.Core.Http;

public static class HttpRequestTemplateResolver
{
    private static readonly Regex TemplatePattern = new(@"\{\{\s*(?<name>[A-Za-z0-9_.-]+)\s*\}\}", RegexOptions.Compiled);

    public static HttpRequestDefinition Resolve(HttpRequestDefinition request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var variables = request.Variables;
        return new HttpRequestDefinition(
            new Uri(ResolveText(request.Url.ToString(), variables), UriKind.Absolute),
            ResolveDictionary(request.Headers, variables),
            ResolveDictionary(request.QueryParameters, variables),
            ResolveBody(request.Body, variables),
            ResolveAuthentication(request.Authentication, variables),
            variables);
    }

    public static string ResolveText(string? value, IReadOnlyDictionary<string, string?>? variables)
    {
        if (string.IsNullOrEmpty(value) || variables is null || variables.Count == 0)
        {
            return value ?? string.Empty;
        }

        return TemplatePattern.Replace(value, match =>
        {
            var key = match.Groups["name"].Value;
            return variables.TryGetValue(key, out var replacement) ? replacement ?? string.Empty : match.Value;
        });
    }

    public static string? ResolveNullableText(string? value, IReadOnlyDictionary<string, string?>? variables)
        => value is null ? null : ResolveText(value, variables);

    public static IReadOnlyDictionary<string, string?>? ResolveDictionary(IReadOnlyDictionary<string, string?>? values, IReadOnlyDictionary<string, string?>? variables)
    {
        if (values is null || values.Count == 0)
        {
            return values;
        }

        var resolved = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in values)
        {
            var key = ResolveText(entry.Key, variables);
            var value = ResolveText(entry.Value, variables);
            resolved[key] = value;
        }

        return resolved;
    }

    public static HttpRequestBody? ResolveBody(HttpRequestBody? body, IReadOnlyDictionary<string, string?>? variables)
        => body switch
        {
            null => null,
            HttpRequestBody.Json json => new HttpRequestBody.RawJson(ResolveText(System.Text.Json.JsonSerializer.Serialize(json.Value), variables), json.ContentType),
            HttpRequestBody.RawJson rawJson => new HttpRequestBody.RawJson(ResolveText(rawJson.Text, variables), rawJson.ContentType),
            HttpRequestBody.Multipart multipart => new HttpRequestBody.Multipart(
                multipart.Parts.Select(part => ResolveMultipartPart(part, variables)).ToArray()),
            _ => throw new NotSupportedException($"Body type '{body.GetType().Name}' is not supported.")
        };

    public static HttpMultipartPart ResolveMultipartPart(HttpMultipartPart part, IReadOnlyDictionary<string, string?>? variables)
    {
        var name = ResolveText(part.Name, variables);
        var fileName = ResolveNullableText(part.FileName, variables);
        var contentType = ResolveNullableText(part.ContentType, variables);

        if (part.FileName is null && LooksLikeTextContent(contentType))
        {
            var text = ResolveText(Encoding.UTF8.GetString(part.Content), variables);
            return new HttpMultipartPart(name, Encoding.UTF8.GetBytes(text), fileName, contentType);
        }

        return new HttpMultipartPart(name, part.Content, fileName, contentType);
    }

    private static bool LooksLikeTextContent(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("form", StringComparison.OrdinalIgnoreCase);
    }

    public static HttpAuthentication? ResolveAuthentication(HttpAuthentication? authentication, IReadOnlyDictionary<string, string?>? variables)
        => authentication switch
        {
            null => null,
            HttpAuthentication.Bearer bearer => new HttpAuthentication.Bearer(ResolveText(bearer.Token, variables)),
            HttpAuthentication.Basic basic => new HttpAuthentication.Basic(ResolveText(basic.UserName, variables), ResolveText(basic.Password, variables)),
            HttpAuthentication.ApiKey apiKey => new HttpAuthentication.ApiKey(ResolveText(apiKey.HeaderName, variables), ResolveText(apiKey.Value, variables)),
            _ => throw new NotSupportedException($"Authentication type '{authentication.GetType().Name}' is not supported.")
        };
}
