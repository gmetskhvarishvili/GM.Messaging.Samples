using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Users;

/// <summary>
/// A user registered. Email and Name are personal data that consumers need (the welcome email, the activity
/// feed), so they travel encrypted ([SensitiveData]): the outbox, the inbox and RabbitMQ only ever hold
/// ciphertext, and handlers get the plain values. They're encrypted with the user's own key ([DataSubject]):
/// deleting that key (an erasure request) makes them unreadable in every copy of the event. Events that
/// don't need personal data carry ids only.
/// </summary>
[IntegrationEventName("users.user-registered")]
[DataSubject(nameof(UserId))] // IntegrationEvent.UserId: the registered user, whose key encrypts Email and Name
public sealed record UserRegisteredIntegrationEvent : IntegrationEvent
{
    public UserRegisteredIntegrationEvent(Guid? userId, string email, string name)
    {
        UserId = userId;
        Email = email;
        Name = name;
    }

    [SensitiveData]
    public string Email { get; init; }

    [SensitiveData]
    public string Name { get; init; }
}
