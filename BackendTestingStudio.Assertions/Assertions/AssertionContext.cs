using System.Net;

namespace BackendTestingStudio.Assertions.Assertions;

public sealed record AssertionContext(
    HttpStatusCode? StatusCode,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? Headers = null,
    string? Body = null,
    double ElapsedMilliseconds = 0);
