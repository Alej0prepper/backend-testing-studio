using BackendTestingStudio.Core.Reporting;
using Microsoft.Extensions.DependencyInjection;

namespace BackendTestingStudio.Reporting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReportingEngine(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IReportEngine, ReportEngine>();
        return services;
    }
}
