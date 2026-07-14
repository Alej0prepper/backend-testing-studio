namespace BackendTestingStudio.Core.History;

public sealed record RequestHistoryEntry
{
    public RequestHistoryEntry(
        Guid id,
        DateTimeOffset createdAt,
        Guid? environmentId,
        string? environmentName,
        RequestHistoryRequestSnapshot request,
        RequestHistoryResponseSnapshot response,
        double elapsedMilliseconds)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CreatedAt = createdAt;
        EnvironmentId = environmentId;
        EnvironmentName = environmentName;
        Request = request;
        Response = response;
        ElapsedMilliseconds = elapsedMilliseconds;
    }

    public Guid Id { get; }

    public DateTimeOffset CreatedAt { get; }

    public Guid? EnvironmentId { get; }

    public string? EnvironmentName { get; }

    public RequestHistoryRequestSnapshot Request { get; }

    public RequestHistoryResponseSnapshot Response { get; }

    public double ElapsedMilliseconds { get; }

    public string Method => Request.Method;

    public string Url => Request.Url;

    public string Status => $"{(int)Response.StatusCode} {Response.StatusCode}";
}
