namespace BackendTestingStudio.Assertions.Assertions;

public sealed record AssertionDefinition
{
    public AssertionDefinition(
        string name,
        AssertionTargetKind target,
        AssertionOperatorKind @operator,
        string? path = null,
        string? expectedValue = null,
        double? maximumMilliseconds = null)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Name is required.", nameof(name)) : name.Trim();
        Target = target;
        Operator = @operator;
        Path = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        ExpectedValue = string.IsNullOrWhiteSpace(expectedValue) ? null : expectedValue.Trim();
        MaximumMilliseconds = maximumMilliseconds;

        Validate();
    }

    public string Name { get; }

    public AssertionTargetKind Target { get; }

    public AssertionOperatorKind Operator { get; }

    public string? Path { get; }

    public string? ExpectedValue { get; }

    public double? MaximumMilliseconds { get; }

    private void Validate()
    {
        if (Target is AssertionTargetKind.Time)
        {
            if (Operator is not AssertionOperatorKind.MaxTime)
            {
                throw new ArgumentException("Time assertions only support the MaxTime operator.", nameof(Operator));
            }

            if (MaximumMilliseconds is null || MaximumMilliseconds < 0)
            {
                throw new ArgumentException("MaximumMilliseconds must be a non-negative value.", nameof(MaximumMilliseconds));
            }

            return;
        }

        if (Target is AssertionTargetKind.StatusCode)
        {
            if (Operator is AssertionOperatorKind.MaxTime)
            {
                throw new ArgumentException("StatusCode assertions do not support MaxTime.", nameof(Operator));
            }

            if (Path is not null)
            {
                throw new ArgumentException("StatusCode assertions do not use Path.", nameof(Path));
            }
        }

        if ((Target is AssertionTargetKind.Header or AssertionTargetKind.JsonPath) && string.IsNullOrWhiteSpace(Path))
        {
            throw new ArgumentException("Path is required for Header and JsonPath assertions.", nameof(Path));
        }

        if (Operator is AssertionOperatorKind.Equals or AssertionOperatorKind.Contains)
        {
            if (ExpectedValue is null)
            {
                throw new ArgumentException("ExpectedValue is required for Equals and Contains assertions.", nameof(ExpectedValue));
            }
        }
    }
}
