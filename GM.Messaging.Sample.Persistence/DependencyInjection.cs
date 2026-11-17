using GM.Messaging.Persistence.Protection;
using GM.Messaging;
using GM.Messaging.Persistence;
using GM.Messaging.Persistence.Outbox;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;
using GM.Messaging.Sample.Domain.SeedWork;
using GM.Messaging.Sample.Persistence.Context;
using GM.Messaging.Sample.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Messaging.Sample.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration, bool runOutboxRelay = false)
    {
        // Pooled contexts with only our own settings: AddGMOutbox (below) attaches GM.Messaging's save interceptors, among them
        // the one that wakes the relays as soon as a save adds outbox messages.
        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ApplicationDatabase"),
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.CommandTimeout(60);
                }).UseSnakeCaseNamingConvention();
        });

        services.AddTransient<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddTransient<IUnitOfWork, UnitOfWork.UnitOfWork>();

        // Everything GM.Messaging needs, in one call: the outbox (administration, erasure events, and the relay in the
        // producer worker or, in the API, the wake-up that makes every relay publish at once), its save interceptors on
        // ApplicationDbContext, and personal data from configuration: the key ring (Messaging:SensitiveData) and the
        // per-person keys in the shared messaging_keys database (ConnectionStrings:SubjectKeys), on the same Npgsql and
        // snake_case settings as ApplicationDbContext.
        services.AddGMOutbox<OutboxMessage, ApplicationDbContext>(configuration, relay: runOutboxRelay);

        return services;
    }
}
