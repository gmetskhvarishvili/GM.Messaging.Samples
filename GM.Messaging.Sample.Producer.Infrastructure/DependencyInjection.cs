using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace GM.Messaging.Sample.Producer.Infrastructure;

public static class DependencyInjection
{
    /// <summary>GM.Messaging (Wolverine + RabbitMQ from configuration).</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<WolverineOptions>? configureWolverine = null)
    {
        services.AddGMMessaging(configuration, configureWolverine);

        return services;
    }
}
