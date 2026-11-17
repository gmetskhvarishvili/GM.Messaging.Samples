using GM.Messaging.Persistence.Protection;
using GM.Messaging.Persistence;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext.Interfaces;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;
using GM.Messaging.Sample.ConsumerA.Domain.SeedWork;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using GM.Messaging.Sample.ConsumerA.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GM.Messaging.Sample.ConsumerA.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Only our own settings: AddGMInbox (below) attaches GM.Messaging's save interceptors to this DbContext.
        services.AddDbContext<ConsumerDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ApplicationDatabase"),
                o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    o.CommandTimeout(60);
                }).UseSnakeCaseNamingConvention();
        });

        services.AddTransient<IInboxMessageRepository, InboxMessageRepository>();
        services.AddTransient<ICustomerActivityRepository, CustomerActivityRepository>();
        services.AddTransient<IConsumerUnitOfWork, UnitOfWork.ConsumerUnitOfWork>();
        // The inbox, its save interceptors on ConsumerDbContext, and personal data from configuration: the key ring
        // (Messaging:SensitiveData, shared with the producer) and the per-person keys in the shared messaging_keys database.
        services.AddGMInbox<InboxMessage, ConsumerDbContext>(configuration);

        return services;
    }
}
