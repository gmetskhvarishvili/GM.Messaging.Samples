using GM.HealthChecks;
using GM.HealthChecks.EntityFramework;
using GM.HealthChecks.Messaging;
using GM.Messaging.AspNetCore;
using GM.Messaging.Sample.Persistence.Context;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Messaging.Sample.Producer.Infrastructure;

public static class DependencyInjection
{
    /// <summary>GM.Messaging (Wolverine + RabbitMQ from configuration) and the health checks.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGMMessaging(configuration);

        // /health/live, /health/ready (database, RabbitMQ, the outbox backlog) and /health/startup.
        services.AddGMHealthChecks()
            .AddGMDatabaseCheck<ApplicationDbContext>()
            .AddGMMessagingCheck()
            .AddGMMessagingBacklogCheck(backlog => backlog.MaxPendingAge = TimeSpan.FromMinutes(2))
            // What waits on RabbitMQ: consumer queues near their limit, dead letters, unrouted messages (GM.Messaging's queue monitor).
            .AddGMBrokerQueueCheck();

        return services;
    }

    /// <summary>
    /// The health endpoints and GM.Messaging's admin endpoints under <c>/admin/messaging</c> (whatever this host has).
    /// The sample has no login, so the admin endpoints are open; a real service passes its own policy:
    /// <c>admin.AuthorizationPolicy = "your-policy"</c>. Broker message bodies show with <c>[SensitiveData]</c> masked.
    /// </summary>
    public static IEndpointRouteBuilder MapInfrastructure(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGMHealthChecks();
        endpoints.MapGMMessagingAdmin(admin =>
        {
            admin.AllowAnonymous = true;
            admin.MessageBodies = BrokerMessageBodies.Redacted;
        });

        return endpoints;
    }
}
