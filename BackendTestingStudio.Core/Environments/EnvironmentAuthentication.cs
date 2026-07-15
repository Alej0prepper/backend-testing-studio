namespace BackendTestingStudio.Core.Environments;

public enum EnvironmentAuthenticationKind
{
    None = 0,
    Bearer = 1,
    Basic = 2,
    ApiKey = 3
}

public abstract record EnvironmentAuthentication
{
    public abstract EnvironmentAuthenticationKind Kind { get; }
}

public sealed record EnvironmentAuthenticationBearer(string Token) : EnvironmentAuthentication
{
    public override EnvironmentAuthenticationKind Kind => EnvironmentAuthenticationKind.Bearer;
}

public sealed record EnvironmentAuthenticationBasic(string UserName, string Password) : EnvironmentAuthentication
{
    public override EnvironmentAuthenticationKind Kind => EnvironmentAuthenticationKind.Basic;
}

public sealed record EnvironmentAuthenticationApiKey(string HeaderName, string Value) : EnvironmentAuthentication
{
    public override EnvironmentAuthenticationKind Kind => EnvironmentAuthenticationKind.ApiKey;
}
