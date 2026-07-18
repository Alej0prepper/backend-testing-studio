using System.Net;

namespace BackendTestingStudio.Core.History;

public sealed record RequestHistoryResponseSnapshot(
    HttpStatusCode StatusCode,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Headers,
    string? Body);
