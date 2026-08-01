using System.Text.Json;
using GM.EntityFramework.Domain.Abstractions;
using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;

public class OutboxMessage : GM.Messaging.Domain.Outbox.OutboxMessage, IAggregateRoot
{
    private OutboxMessage() { }

    public new static OutboxMessage From<TEvent>(TEvent evt, JsonSerializerOptions? options = null)
        where TEvent : IIntegrationEvent =>
        new()
        {
            EventType = evt.GetType().AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(evt, evt.GetType(), options),
            UserId = evt.UserId
        };
}
