using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Customers;

/// <summary>
/// A reminder for a customer, scheduled when it was requested: its outbox row has a PublishAt time, so the relay
/// publishes it only once that time comes. ConsumerA records it in the customer's activity feed.
/// </summary>
[IntegrationEventName("customers.reminder-due")]
public sealed record ReminderDueIntegrationEvent(Guid CustomerId, string Text, DateTime DueAtUtc) : IntegrationEvent;
