using GM.EntityFramework.Persistence.Repositories;
using GM.Messaging.Persistence.Outbox;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;
using GM.Messaging.Sample.Persistence.Context;

namespace GM.Messaging.Sample.Persistence.Repositories;

public class OutboxMessageRepository(ApplicationDbContext context)
    : GenericRepository<OutboxMessage, ApplicationDbContext>(context), IOutboxMessageRepository, IOutboxDbContext<OutboxMessage>
{
    IQueryable<OutboxMessage> IOutboxDbContext<OutboxMessage>.OutboxMessages => context.Set<OutboxMessage>();

    Task<int> IOutboxDbContext<OutboxMessage>.SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
