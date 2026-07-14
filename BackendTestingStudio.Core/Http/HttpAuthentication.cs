namespace BackendTestingStudio.Core.Http;

public abstract record HttpAuthentication
{
    protected HttpAuthentication()
    {
    }

    public sealed record Bearer(string Token) : HttpAuthentication;

    public sealed record Basic(string UserName, string Password) : HttpAuthentication;

    public sealed record ApiKey(string HeaderName, string Value) : HttpAuthentication;
}
