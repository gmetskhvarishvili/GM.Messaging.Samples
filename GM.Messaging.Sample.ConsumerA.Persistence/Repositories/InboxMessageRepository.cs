using GM.EntityFramework.Persistence.Repositories;
using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate.Interfaces;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GM.Messaging.Sample.ConsumerA.Persistence.Repositories;

public class InboxMessageRepository(ConsumerDbContext context)
    : GenericRepository<InboxMessage, ConsumerDbContext>(context), IInboxMessageRepository, IInboxStore<InboxMessage>
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
