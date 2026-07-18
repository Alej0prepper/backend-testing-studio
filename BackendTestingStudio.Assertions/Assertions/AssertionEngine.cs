using System.Text.Json;
using BackendTestingStudio.Core.Assertions;

namespace BackendTestingStudio.Assertions.Assertions;

public sealed class AssertionEngine : IAssertionEngine
{
    public AssertionResult Evaluate(AssertionDefinition assertion, AssertionContext context)
    {
        ArgumentNullException.ThrowIfNull(assertion);
        ArgumentNullException.ThrowIfNull(context);

        return assertion.Target switch
        {
            AssertionTargetKind.StatusCode => EvaluateStatusCode(assertion, context),
            AssertionTargetKind.Header => EvaluateHeader(assertion, context),
            AssertionTargetKind.JsonPath => EvaluateJsonPath(assertion, context),
            AssertionTargetKind.Time => EvaluateTime(assertion, context),
            _ => throw new NotSupportedException($"Target '{assertion.Target}' is not supported.")
        };
    }

    public IReadOnlyList<AssertionResult> Evaluate(IEnumerable<AssertionDefinition> assertions, AssertionContext context)
    {
        ArgumentNullException.ThrowIfNull(assertions);
        ArgumentNullException.ThrowIfNull(context);

        return assertions.Select(assertion => Evaluate(assertion, context)).ToArray();
    }

    private static AssertionResult EvaluateStatusCode(AssertionDefinition assertion, AssertionContext context)
    {
        var actual = context.StatusCode is null ? null : ((int)context.StatusCode.Value).ToString();
        return EvaluateScalarAssertion(assertion, actual, context.StatusCode is not null);
    }

    private static AssertionResult EvaluateHeader(AssertionDefinition assertion, AssertionContext context)
    {
        var headers = context.Headers is null
            ? []
            : context.Headers.TryGetValue(assertion.Path!, out var values) ? values : [];

        return assertion.Operator switch
        {
            AssertionOperatorKind.Null => BuildResult(assertion, headers.Count == 0, null, "Header was not present."),
            AssertionOperatorKind.NotNull => BuildResult(assertion, headers.Count > 0, JoinValues(headers), "Header was not present."),
            AssertionOperatorKind.Equals => BuildResult(
                assertion,
                headers.Any(value => string.Equals(value, assertion.ExpectedValue, StringComparison.OrdinalIgnoreCase)),
                JoinValues(headers),
                $"Expected header '{assertion.Path}' to equal '{assertion.ExpectedValue}'."),
            AssertionOperatorKind.Contains => BuildResult(
                assertion,
                headers.Any(value => value?.Contains(assertion.ExpectedValue!, StringComparison.OrdinalIgnoreCase) == true),
                JoinValues(headers),
                $"Expected header '{assertion.Path}' to contain '{assertion.ExpectedValue}'."),
            _ => UnsupportedOperator(assertion)
        };
    }

    private static AssertionResult EvaluateJsonPath(AssertionDefinition assertion, AssertionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.Body))
        {
            return assertion.Operator switch
            {
                AssertionOperatorKind.Null => BuildResult(assertion, true, null, "JSON body was empty."),
                AssertionOperatorKind.NotNull => BuildResult(assertion, false, null, "JSON body was empty."),
                _ => BuildResult(assertion, false, null, "JSON body was empty.")
            };
        }

        try
        {
            var values = JsonPathReader.ReadValues(context.Body, assertion.Path!);
            var actual = JoinValues(values);

            return assertion.Operator switch
            {
                AssertionOperatorKind.Null => BuildResult(assertion, values.Count == 0, actual, $"Expected JSONPath '{assertion.Path}' to be null."),
                AssertionOperatorKind.NotNull => BuildResult(assertion, values.Count > 0, actual, $"Expected JSONPath '{assertion.Path}' to be not null."),
                AssertionOperatorKind.Equals => BuildResult(
                    assertion,
                    values.Any(value => string.Equals(value, assertion.ExpectedValue, StringComparison.OrdinalIgnoreCase)),
                    actual,
                    $"Expected JSONPath '{assertion.Path}' to equal '{assertion.ExpectedValue}'."),
                AssertionOperatorKind.Contains => BuildResult(
                    assertion,
                    values.Any(value => value.Contains(assertion.ExpectedValue!, StringComparison.OrdinalIgnoreCase)),
                    actual,
                    $"Expected JSONPath '{assertion.Path}' to contain '{assertion.ExpectedValue}'."),
                _ => UnsupportedOperator(assertion)
            };
        }
        catch (ArgumentException ex)
        {
            return new AssertionResult(
                assertion.Name,
                false,
                $"Invalid JSONPath for assertion '{assertion.Name}': {ex.Message}");
        }
        catch (JsonException ex)
        {
            return new AssertionResult(
                assertion.Name,
                false,
                $"Invalid JSON body for assertion '{assertion.Name}': {ex.Message}");
        }
    }

    private static AssertionResult EvaluateTime(AssertionDefinition assertion, AssertionContext context)
    {
        if (assertion.MaximumMilliseconds is null)
        {
            return new AssertionResult(assertion.Name, false, "MaximumMilliseconds was not configured.");
        }

        var passed = context.ElapsedMilliseconds <= assertion.MaximumMilliseconds.Value;
        return new AssertionResult(
            assertion.Name,
            passed,
            passed
                ? $"Elapsed time {context.ElapsedMilliseconds:0.##} ms is within the limit of {assertion.MaximumMilliseconds:0.##} ms."
                : $"Elapsed time {context.ElapsedMilliseconds:0.##} ms exceeded the limit of {assertion.MaximumMilliseconds:0.##} ms.",
            context.ElapsedMilliseconds.ToString("0.##"),
            assertion.MaximumMilliseconds.Value.ToString("0.##"));
    }

    private static AssertionResult EvaluateScalarAssertion(AssertionDefinition assertion, string? actual, bool hasValue)
    {
        return assertion.Operator switch
        {
            AssertionOperatorKind.Null => BuildResult(assertion, !hasValue || string.IsNullOrWhiteSpace(actual), actual, "Expected value to be null."),
            AssertionOperatorKind.NotNull => BuildResult(assertion, hasValue && !string.IsNullOrWhiteSpace(actual), actual, "Expected value to be not null."),
            AssertionOperatorKind.Equals => BuildResult(assertion, string.Equals(actual, assertion.ExpectedValue, StringComparison.OrdinalIgnoreCase), actual, $"Expected '{assertion.ExpectedValue}'."),
            AssertionOperatorKind.Contains => BuildResult(assertion, actual?.Contains(assertion.ExpectedValue!, StringComparison.OrdinalIgnoreCase) == true, actual, $"Expected to contain '{assertion.ExpectedValue}'."),
            _ => UnsupportedOperator(assertion)
        };
    }

    private static AssertionResult UnsupportedOperator(AssertionDefinition assertion)
        => new(assertion.Name, false, $"Operator '{assertion.Operator}' is not supported for target '{assertion.Target}'.");

    private static AssertionResult BuildResult(AssertionDefinition assertion, bool passed, string? actualValue, string failureMessage)
        => new(
            assertion.Name,
            passed,
            passed ? $"Assertion '{assertion.Name}' passed." : failureMessage,
            actualValue,
            assertion.ExpectedValue ?? (assertion.MaximumMilliseconds is null ? null : assertion.MaximumMilliseconds.Value.ToString("0.##")));

    private static string JoinValues(IEnumerable<string?> values)
        => string.Join(", ", values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!.Trim()));

}
