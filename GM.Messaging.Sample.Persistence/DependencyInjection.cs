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
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // shared service caching and is discouraged unless you inject custom EF services.
        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ApplicationDatabase"),
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.CommandTimeout(60);
                });
        });

        services.AddTransient<OutboxMessageRepository>();
        services.AddTransient<IOutboxMessageRepository>(sp => sp.GetRequiredService<OutboxMessageRepository>());
        services.AddTransient<IOutboxDbContext<OutboxMessage>>(sp => sp.GetRequiredService<OutboxMessageRepository>());
        services.AddTransient<IUnitOfWork, UnitOfWork.UnitOfWork>();

        return services;
    }
}
