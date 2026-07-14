namespace BackendTestingStudio.Core.History;

public sealed record RequestHistoryMultipartPart(
    string Name,
    string Value,
    string? FileName = null,
    string? ContentType = null);
