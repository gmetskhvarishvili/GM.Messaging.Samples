using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Users;

public sealed record UserRegisteredIntegrationEvent : IntegrationEvent
{
    public UserRegisteredIntegrationEvent(Guid? userId, string email, string name)
    {
        UserId = userId;
        Email = email;
        Name = name;
    }

    public string Email { get; init; }
    public string Name { get; init; }
}
