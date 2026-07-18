using System.Net;
using BackendTestingStudio.Assertions.Assertions;
using BackendTestingStudio.Core.Assertions;
using Xunit;

namespace BackendTestingStudio.Assertions.Tests;

public sealed class AssertionEngineTests
{
    private readonly IAssertionEngine _engine = new AssertionEngine();

    [Fact]
    public void StatusCode_Equals_Passes()
    {
        var result = _engine.Evaluate(
            new AssertionDefinition(
                "status",
                AssertionTargetKind.StatusCode,
                AssertionOperatorKind.Equals,
                expectedValue: "200"),
            new AssertionContext(HttpStatusCode.OK));

        Assert.True(result.Passed);
        Assert.Contains("passed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Header_Contains_Passes()
    {
        var result = _engine.Evaluate(
            new AssertionDefinition(
                "header",
                AssertionTargetKind.Header,
                AssertionOperatorKind.Contains,
                path: "content-type",
                expectedValue: "json"),
            new AssertionContext(
                HttpStatusCode.OK,
                new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["content-type"] = ["application/json; charset=utf-8"]
                }));

        Assert.True(result.Passed);
        Assert.Equal("application/json; charset=utf-8", result.ActualValue);
    }

    [Fact]
    public void Header_Null_PassesWhenMissing()
    {
        var result = _engine.Evaluate(
            new AssertionDefinition(
                "missing-header",
                AssertionTargetKind.Header,
                AssertionOperatorKind.Null,
                path: "x-missing"),
            new AssertionContext(HttpStatusCode.OK));

        Assert.True(result.Passed);
    }

    [Fact]
    public void JsonPath_Equals_Passes()
    {
        var result = _engine.Evaluate(
            new AssertionDefinition(
                "jsonpath",
                AssertionTargetKind.JsonPath,
                AssertionOperatorKind.Equals,
                path: "$.customer.name",
                expectedValue: "Alice"),
            new AssertionContext(
                HttpStatusCode.OK,
                Body: """
                    {
                      "customer": {
                        "name": "Alice"
                      }
                    }
                    """));

        Assert.True(result.Passed);
        Assert.Equal("Alice", result.ActualValue);
    }

    [Fact]
    public void JsonPath_Wildcard_Contains_Passes()
    {
        var result = _engine.Evaluate(
            new AssertionDefinition(
                "jsonpath-list",
                AssertionTargetKind.JsonPath,
                AssertionOperatorKind.Contains,
                path: "$.items[*].name",
                expectedValue: "Pen"),
            new AssertionContext(
                HttpStatusCode.OK,
                Body: """
                    {
                      "items": [
                        { "name": "Book" },
                        { "name": "Pen" }
                      ]
                    }
                    """));

        Assert.True(result.Passed);
        Assert.Contains("Pen", result.ActualValue, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Time_MaxTime_FailsWhenElapsedExceedsLimit()
    {
        var result = _engine.Evaluate(
            new AssertionDefinition(
                "time",
                AssertionTargetKind.Time,
                AssertionOperatorKind.MaxTime,
                maximumMilliseconds: 120),
            new AssertionContext(HttpStatusCode.OK, ElapsedMilliseconds: 245));

        Assert.False(result.Passed);
        Assert.Contains("exceeded", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Evaluate_All_ReturnsAllResults()
    {
        var results = _engine.Evaluate(
            [
                new AssertionDefinition("status", AssertionTargetKind.StatusCode, AssertionOperatorKind.Equals, expectedValue: "200"),
                new AssertionDefinition("elapsed", AssertionTargetKind.Time, AssertionOperatorKind.MaxTime, maximumMilliseconds: 300)
            ],
            new AssertionContext(HttpStatusCode.OK, ElapsedMilliseconds: 120));

        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.True(result.Passed));
    }
}
