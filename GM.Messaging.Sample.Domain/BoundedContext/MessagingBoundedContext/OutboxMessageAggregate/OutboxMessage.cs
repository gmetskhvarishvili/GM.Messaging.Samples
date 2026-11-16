using System.Text.Json;
using GM.EntityFramework.Domain.Abstractions;
using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;

public class OutboxMessage : GM.Messaging.Domain.Outbox.OutboxMessage, IAggregateRoot
{
    private OutboxMessage() { }

    /// <summary>
    /// The event as an outbox message: its stable name and version, payload, ordering key (from
    /// <see cref="IOrderedIntegrationEvent"/>) and the current trace, so the relay continues the request's trace.
    /// </summary>
    public new static OutboxMessage From<TEvent>(TEvent evt, JsonSerializerOptions? options = null, string? orderingKey = null)
        where TEvent : IIntegrationEvent
    {
        var message = new OutboxMessage();
        message.Populate(evt, options, orderingKey);
        return message;
    }
}
