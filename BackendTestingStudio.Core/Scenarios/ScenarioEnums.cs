namespace BackendTestingStudio.Core.Scenarios;

public enum ScenarioHttpMethod
{
    Get,
    Post,
    Put,
    Patch,
    Delete
}

public enum ScenarioVariableSource
{
    JsonPath,
    Header,
    StatusCode,
    Body
}

public enum ScenarioStepStatus
{
    Succeeded,
    Failed,
    Skipped
}

public enum ScenarioExecutionStatus
{
    Succeeded,
    Failed
}
