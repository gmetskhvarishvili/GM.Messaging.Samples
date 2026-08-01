using GM.Messaging.Persistence;
using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;
using GM.Messaging.Sample.ConsumerB.Domain.SeedWork;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;
using GM.Messaging.Sample.ConsumerB.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Messaging.Sample.ConsumerB.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConsumerDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ApplicationDatabase"),
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.CommandTimeout(60);
                });
        });

        services.AddTransient<InboxMessageRepository>();
        services.AddTransient<IInboxMessageRepository, InboxMessageRepository>();
        services.AddTransient<IInboxStore<InboxMessage>, InboxMessageRepository>();
        services.AddTransient<IConsumerUnitOfWork, UnitOfWork.ConsumerUnitOfWork>();
        services.AddGMInboxProcessor<InboxMessage>();

        return services;
    }
}
