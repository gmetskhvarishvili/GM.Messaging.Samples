using GM.EntityFramework.Domain.Abstractions;

namespace GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.MessagingBoundedContext.InboxMessageAggregate;

/// <summary>
/// The consumer's inbox row. GM.Messaging creates it (InboxMessage.Create&lt;T&gt;, which only needs this
/// private constructor) and records attempts, failures and the inbox worker's state on it.
/// </summary>
public class InboxMessage : GM.Messaging.Domain.Inbox.InboxMessage, IAggregateRoot
{
    private InboxMessage() { }
}
