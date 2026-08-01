using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Orders;

public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency) : IntegrationEvent;
