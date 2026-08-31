using GM.EntityFramework.Persistence.Repositories;
using GM.Messaging.Persistence.Outbox;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate.Interfaces;
using GM.Messaging.Sample.Persistence.Context;

namespace GM.Messaging.Sample.Persistence.Repositories;

// CS9107: 'context' is forwarded to GenericRepository's base constructor and also read directly
// below for the IOutboxDbContext<T> members the base type doesn't expose. Both refer to the same
// DbContext instance, so there is no divergent-copy risk; the warning exists only because the
// compiler cannot see across the assembly boundary into GenericRepository's own capture.
#pragma warning disable CS9107
public class OutboxMessageRepository(ApplicationDbContext context)
    : GenericRepository<OutboxMessage, ApplicationDbContext>(context), IOutboxMessageRepository, IOutboxDbContext<OutboxMessage>
#pragma warning restore CS9107
{
    IQueryable<OutboxMessage> IOutboxDbContext<OutboxMessage>.OutboxMessages => context.Set<OutboxMessage>();

    Task<int> IOutboxDbContext<OutboxMessage>.SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
