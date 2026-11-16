using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Orders;

/// <summary>
/// An order was placed. Stored and routed under the stable name "orders.order-placed", so the class
/// can be renamed or moved without stranding outbox rows or breaking ProducerTopology. Ordered by
/// order: an order's events are published, and handled by ConsumerB, in the order they were raised.
/// </summary>
[IntegrationEventName("orders.order-placed")]
public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency) : IntegrationEvent, IOrderedIntegrationEvent
{
    public string? OrderingKey => OrderId.ToString();
}
