using GM.Messaging.Sample.Common.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace GM.Messaging.Sample.ConsumerA.Infrastructure;

public static class DependencyInjection
{
    /// <summary>GM.Messaging (Wolverine + RabbitMQ from configuration) and OpenTelemetry.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<WolverineOptions>? configureWolverine = null)
    {
        services.AddGMMessaging(configuration, configureWolverine);
        services.AddSampleTelemetry(configuration);

        return services;
    }
}
