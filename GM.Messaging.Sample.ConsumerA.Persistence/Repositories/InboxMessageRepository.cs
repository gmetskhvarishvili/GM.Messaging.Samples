using GM.EntityFramework.Persistence.Repositories;
using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Repositories;

// CS9107: 'context' is forwarded to GenericRepository's base constructor and also read directly
// below for the IInboxStore<T> members the base type doesn't expose. Both refer to the same
// DbContext instance, so there is no divergent-copy risk; the warning exists only because the
// compiler cannot see across the assembly boundary into GenericRepository's own capture.
#pragma warning disable CS9107
public class InboxMessageRepository(ConsumerDbContext context)
    : GenericRepository<InboxMessage, ConsumerDbContext>(context), IInboxMessageRepository, IInboxStore<InboxMessage>
#pragma warning restore CS9107
{
    Task<bool> IInboxStore<InboxMessage>.ExistsAsync(Guid eventId, string consumerName, CancellationToken cancellationToken) =>
        context.Set<InboxMessage>().AnyAsync(m => m.EventId == eventId && m.ConsumerName == consumerName, cancellationToken);

    async Task<InboxMessage> IInboxStore<InboxMessage>.CreateAndAddAsync(
        Guid eventId, string consumerName, string eventType, string payload, Guid? userId, CancellationToken cancellationToken)
    {
        var message = InboxMessage.Create(eventId, consumerName, eventType, payload, userId);
        await context.Set<InboxMessage>().AddAsync(message, cancellationToken);
        return message;
    }

    Task IInboxStore<InboxMessage>.SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
