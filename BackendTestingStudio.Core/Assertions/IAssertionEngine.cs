namespace BackendTestingStudio.Core.Assertions;

public interface IAssertionEngine
{
    AssertionResult Evaluate(AssertionDefinition assertion, AssertionContext context);

    IReadOnlyList<AssertionResult> Evaluate(IEnumerable<AssertionDefinition> assertions, AssertionContext context);
}
