using System.Text.Json;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events.Orders;
using Xunit;

namespace GM.Messaging.Sample.Tests;

public class OutboxMessageTests
{
    [Fact]
    public void From_serializes_the_event_and_captures_its_type()
    {
        var evt = new OrderPlacedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), 99.9m, "EUR");

        var outbox = OutboxMessage.From(evt);

        Assert.Equal(typeof(OrderPlacedIntegrationEvent).AssemblyQualifiedName, outbox.EventType);

        var roundtrip = JsonSerializer.Deserialize<OrderPlacedIntegrationEvent>(outbox.Payload);
        Assert.NotNull(roundtrip);
        Assert.Equal("EUR", roundtrip.Currency);
        Assert.Equal(evt.OrderId, roundtrip.OrderId);
    }
}
