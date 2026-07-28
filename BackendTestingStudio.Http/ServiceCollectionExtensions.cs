using BackendTestingStudio.Core.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BackendTestingStudio.Http;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendTestingStudioHttp(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient<IHttpEngine, HttpEngine>(client =>
            {
                client.Timeout = Timeout.InfiniteTimeSpan;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false
            });
        return services;
    }
}
