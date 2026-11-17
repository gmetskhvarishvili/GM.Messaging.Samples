using GM.HealthChecks.EntityFramework;
using GM.HealthChecks.Messaging;
using GM.Messaging.AspNetCore;
using GM.Messaging.Sample.Persistence.Context;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Messaging.Sample.Infrastructure;

public static class DependencyInjection
{
    /// <summary>The health checks (GM.API maps their endpoints).</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Readiness (GM.API maps /health/live, /health/ready and /health/startup) also checks the database and the
        // outbox backlog: pending messages, the oldest one's age and dead letters report Degraded when the relay
        // falls behind or gives up on messages. The API only writes the outbox; the producer workers publish it.
        services.AddHealthChecks()
            .AddGMDatabaseCheck<ApplicationDbContext>()
            .AddGMMessagingBacklogCheck(backlog => backlog.MaxPendingAge = TimeSpan.FromMinutes(2));

        return services;
    }

    /// <summary>
    /// GM.Messaging's admin endpoints under <c>/admin/messaging</c> (this host's outbox and subject keys).
    /// The sample has no login, so the admin endpoints are open; a real service passes its own policy:
    /// <c>admin.AuthorizationPolicy = "your-policy"</c>. Broker message bodies show with <c>[SensitiveData]</c> masked.
    /// </summary>
    public static IEndpointRouteBuilder MapInfrastructure(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGMMessagingAdmin(admin =>
        {
            admin.AllowAnonymous = true;
            admin.MessageBodies = BrokerMessageBodies.Redacted;
        });

        return endpoints;
    }
}
