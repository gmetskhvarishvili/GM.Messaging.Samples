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
        // Pooled contexts; UseGMMessaging adds GM.Messaging's interceptors, among them the one that wakes the relays as soon as a save
        // adds outbox messages: this process's relay directly, relays in other processes over RabbitMQ.
        services.AddDbContextPool<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ApplicationDatabase"),
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.CommandTimeout(60);
                }).UseSnakeCaseNamingConvention()
                .UseGMMessaging(serviceProvider);
        });

        services.AddTransient<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddTransient<IUnitOfWork, UnitOfWork.UnitOfWork>();

        // Personal data in events: the key ring (Messaging:SensitiveData) and the per-person keys in the shared
        // messaging_keys database (ConnectionStrings:SubjectKeys), which GM.Messaging creates at startup.
        services.AddGMSensitiveData(configuration, subjectKeys: (db, connectionString) =>
            db.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

        // The outbox: administration (/admin/messaging/outbox), erasure events (gm.subject-forgotten), and the relay in
        // the producer worker or, in the API, the wake-up that makes every relay publish at once.
        services.AddGMOutbox<OutboxMessage, ApplicationDbContext>(configuration, relay: runOutboxRelay);

        return services;
    }
}
