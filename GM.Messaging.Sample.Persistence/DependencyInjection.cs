using GM.Messaging;
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
        // shared service caching and is discouraged unless you inject custom EF services.
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

        // Only the process that relays the outbox (the producer worker) passes true; the API must not,
        // or two pollers would read the same outbox table and publish messages twice.
        if (runOutboxRelay)
        {
            services.AddGMOutboxRelay<OutboxMessage, ApplicationDbContext>();
        }

        return services;
    }
}
