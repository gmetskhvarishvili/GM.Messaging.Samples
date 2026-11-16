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
        // Pooled contexts; AddGMOutboxSignal adds the interceptor that wakes this process's relay as soon
        // as a save adds outbox messages (a no-op in a process that doesn't run the relay).
        services.AddDbContextPool<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ApplicationDatabase"),
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.CommandTimeout(60);
                }).UseSnakeCaseNamingConvention()
                .AddGMOutboxSignal(serviceProvider);
        });

        services.AddTransient<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddTransient<IUnitOfWork, UnitOfWork.UnitOfWork>();

        // Every host can inspect the outbox and requeue dead letters (the API exposes it under /admin).
        services.AddGMOutboxAdministration<OutboxMessage, ApplicationDbContext>();

        // The relay leases each message, so several relays are safe; the producer worker runs it here.
        if (runOutboxRelay)
            services.AddGMOutboxRelay<OutboxMessage, ApplicationDbContext>();

        return services;
    }
}
