using BackendTestingStudio.Core.Http;

namespace BackendTestingStudio.Core.Environments;

public static class EnvironmentAuthenticationExtensions
{
    public static HttpAuthentication? ToHttpAuthentication(this EnvironmentAuthentication? authentication)
        => authentication switch
        {
            null => null,
            EnvironmentAuthenticationBearer bearer => new HttpAuthentication.Bearer(bearer.Token),
            EnvironmentAuthenticationBasic basic => new HttpAuthentication.Basic(basic.UserName, basic.Password),
            EnvironmentAuthenticationApiKey apiKey => new HttpAuthentication.ApiKey(apiKey.HeaderName, apiKey.Value),
            _ => throw new NotSupportedException($"Authentication type '{authentication.GetType().Name}' is not supported.")
        };
}
