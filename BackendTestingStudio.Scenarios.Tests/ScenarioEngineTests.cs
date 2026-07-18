using System.Net;
using BackendTestingStudio.Assertions.Assertions;
using BackendTestingStudio.Core.Assertions;
using BackendTestingStudio.Core.Http;
using BackendTestingStudio.Core.Scenarios;
using BackendTestingStudio.Scenarios.Scenarios;
using Xunit;

namespace BackendTestingStudio.Scenarios.Tests;

public sealed class ScenarioEngineTests
{
    [Fact]
    public async Task ExecuteAsync_CapturesVariableAndUsesItInLaterStep()
    {
        var http = new FakeHttpEngine(
            Response(HttpStatusCode.Created, """{"id":"order-42"}"""),
            Response(HttpStatusCode.OK, """{"id":"order-42"}"""));
        var engine = CreateEngine(http);
        var scenario = new ScenarioDefinition(
            "order-flow",
            "Order flow",
            [
                new ScenarioStepDefinition(
                    "Create order",
                    ScenarioHttpMethod.Post,
                    Request("https://api.test/orders"),
                    saveVariables:
                    [
                        new ScenarioVariableCapture("OrderId", ScenarioVariableSource.JsonPath, "$.id")
                    ]),
                new ScenarioStepDefinition(
                    "Get order",
                    ScenarioHttpMethod.Get,
                    Request("https://api.test/orders/{{OrderId}}"),
                    assertions:
                    [
                        new AssertionDefinition(
                            "same order",
                            AssertionTargetKind.JsonPath,
                            AssertionOperatorKind.Equals,
                            "$.id",
                            "{{OrderId}}")
                    ])
            ]);

        var result = await engine.ExecuteAsync(scenario);

        Assert.Equal(ScenarioExecutionStatus.Succeeded, result.Status);
        Assert.Equal("order-42", result.Variables["OrderId"]);
        Assert.Equal("https://api.test/orders/order-42", http.Requests[1].Url.ToString());
        Assert.All(result.Steps, step => Assert.Equal(ScenarioStepStatus.Succeeded, step.Status));
    }

    [Fact]
    public async Task ExecuteAsync_StopsAfterFailedAssertionWhenConfigured()
    {
        var http = new FakeHttpEngine(
            Response(HttpStatusCode.BadRequest),
            Response(HttpStatusCode.OK));
        var engine = CreateEngine(http);
        var scenario = new ScenarioDefinition(
            "stop-on-failure",
            "Stop on failure",
            [
                StepWithStatusAssertion("Failing step", HttpStatusCode.OK, stopOnFailure: true),
                new ScenarioStepDefinition("Never executed", ScenarioHttpMethod.Get, Request("https://api.test/next"))
            ]);

        var result = await engine.ExecuteAsync(scenario);

        Assert.Equal(ScenarioExecutionStatus.Failed, result.Status);
        Assert.True(result.StoppedEarly);
        Assert.Single(result.Steps);
        Assert.Single(http.Requests);
        Assert.False(result.Steps[0].Assertions[0].Passed);
    }

    [Fact]
    public async Task ExecuteAsync_ContinuesAfterFailureWhenStopIsDisabled()
    {
        var http = new FakeHttpEngine(
            Response(HttpStatusCode.BadRequest),
            Response(HttpStatusCode.OK));
        var engine = CreateEngine(http);
        var scenario = new ScenarioDefinition(
            "continue-on-failure",
            "Continue on failure",
            [
                StepWithStatusAssertion("Failing step", HttpStatusCode.OK, stopOnFailure: false),
                StepWithStatusAssertion("Successful step", HttpStatusCode.OK, stopOnFailure: true)
            ]);

        var result = await engine.ExecuteAsync(scenario);

        Assert.Equal(ScenarioExecutionStatus.Failed, result.Status);
        Assert.False(result.StoppedEarly);
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(2, http.Requests.Count);
        Assert.Equal(ScenarioStepStatus.Succeeded, result.Steps[1].Status);
    }

    [Fact]
    public async Task ExecuteAsync_FailsWhenRequiredVariableCannotBeCaptured()
    {
        var http = new FakeHttpEngine(Response(HttpStatusCode.OK, "{}"));
        var engine = CreateEngine(http);
        var scenario = new ScenarioDefinition(
            "required-capture",
            "Required capture",
            [
                new ScenarioStepDefinition(
                    "Capture",
                    ScenarioHttpMethod.Get,
                    Request("https://api.test/item"),
                    saveVariables:
                    [
                        new ScenarioVariableCapture("ProductId", ScenarioVariableSource.JsonPath, "$.id")
                    ])
            ]);

        var result = await engine.ExecuteAsync(scenario);

        Assert.Equal(ScenarioExecutionStatus.Failed, result.Status);
        Assert.Contains("ProductId", result.Steps[0].Error);
        Assert.False(result.Variables.ContainsKey("ProductId"));
    }

    [Theory]
    [InlineData(ScenarioHttpMethod.Get, "GET")]
    [InlineData(ScenarioHttpMethod.Post, "POST")]
    [InlineData(ScenarioHttpMethod.Put, "PUT")]
    [InlineData(ScenarioHttpMethod.Patch, "PATCH")]
    [InlineData(ScenarioHttpMethod.Delete, "DELETE")]
    public async Task ExecuteAsync_DispatchesSupportedHttpMethods(ScenarioHttpMethod method, string expected)
    {
        var http = new FakeHttpEngine(Response(HttpStatusCode.OK));
        var engine = CreateEngine(http);
        var scenario = new ScenarioDefinition(
            "method",
            "Method",
            [new ScenarioStepDefinition("Execute", method, Request("https://api.test/resource"))]);

        var result = await engine.ExecuteAsync(scenario);

        Assert.Equal(ScenarioExecutionStatus.Succeeded, result.Status);
        Assert.Equal(expected, http.Methods.Single());
    }

    [Fact]
    public async Task ExecuteAsync_RecordsDisabledStepAsSkipped()
    {
        var http = new FakeHttpEngine();
        var engine = CreateEngine(http);
        var scenario = new ScenarioDefinition(
            "disabled",
            "Disabled",
            [new ScenarioStepDefinition("Disabled step", ScenarioHttpMethod.Get, Request("https://api.test"), enabled: false)]);

        var result = await engine.ExecuteAsync(scenario);

        Assert.Equal(ScenarioExecutionStatus.Succeeded, result.Status);
        Assert.Equal(ScenarioStepStatus.Skipped, result.Steps[0].Status);
        Assert.Empty(http.Requests);
    }

    private static ScenarioEngine CreateEngine(IHttpEngine httpEngine)
        => new(httpEngine, new AssertionEngine());

    private static ScenarioStepDefinition StepWithStatusAssertion(
        string name,
        HttpStatusCode expected,
        bool stopOnFailure)
        => new(
            name,
            ScenarioHttpMethod.Get,
            Request("https://api.test/status"),
            [
                new AssertionDefinition(
                    "status",
                    AssertionTargetKind.StatusCode,
                    AssertionOperatorKind.Equals,
                    expectedValue: ((int)expected).ToString())
            ],
            stopOnFailure: stopOnFailure);

    private static HttpRequestDefinition Request(string url) => new(new Uri(url));

    private static HttpResponseResult Response(HttpStatusCode statusCode, string? body = null)
        => new(statusCode, body, new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase));

    private sealed class FakeHttpEngine(params HttpResponseResult[] responses) : IHttpEngine
    {
        private readonly Queue<HttpResponseResult> _responses = new(responses);

        public List<HttpRequestDefinition> Requests { get; } = [];

        public List<string> Methods { get; } = [];

        public Task<HttpResponseResult> GetAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => ExecuteAsync("GET", request);

        public Task<HttpResponseResult> PostAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => ExecuteAsync("POST", request);

        public Task<HttpResponseResult> PutAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => ExecuteAsync("PUT", request);

        public Task<HttpResponseResult> PatchAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => ExecuteAsync("PATCH", request);

        public Task<HttpResponseResult> DeleteAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
            => ExecuteAsync("DELETE", request);

        private Task<HttpResponseResult> ExecuteAsync(string method, HttpRequestDefinition request)
        {
            Methods.Add(method);
            Requests.Add(request);
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
