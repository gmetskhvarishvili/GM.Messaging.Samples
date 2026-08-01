using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Payments;

public sealed record PaymentProcessedIntegrationEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Status) : IntegrationEvent;
