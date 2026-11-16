using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Payments;

/// <summary>A payment for an order was processed; ordered with the order's other events.</summary>
[IntegrationEventName("payments.payment-processed")]
public sealed record PaymentProcessedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Status) : IntegrationEvent, IOrderedIntegrationEvent
{
    public string? OrderingKey => OrderId.ToString();
}
