namespace BackendTestingStudio.Core.Assertions;

public enum AssertionTargetKind
{
    StatusCode,
    Header,
    JsonPath,
    Time
}

public enum AssertionOperatorKind
{
    Equals,
    Contains,
    Null,
    NotNull,
    MaxTime
}
