using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Returns;

/// <summary>
/// A customer asked to return an order. The API publishes it before any returns service exists, so RabbitMQ
/// keeps it in gm.unrouted until one binds a queue (CI then moves it there). The contact email is personal
/// data, encrypted with the customer's key: the admin endpoints show such messages with it masked.
/// </summary>
[IntegrationEventName("returns.return-requested")]
public sealed record ReturnRequestedIntegrationEvent(
    Guid ReturnId,
    Guid OrderId,
    [property: DataSubject] Guid CustomerId,
    [property: SensitiveData] string ContactEmail,
    string Reason) : IntegrationEvent;
