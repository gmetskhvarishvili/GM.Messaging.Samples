using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.Domain.Events.Users;
using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

public class UserRegisteredHandler(IInboxProcessor inbox, ILogger<UserRegisteredHandler> logger)
{
    public Task Handle(UserRegisteredIntegrationEvent message, CancellationToken ct) =>
        inbox.ProcessAsync(message, "ConsumerA", () =>
        {
            logger.LogInformation(
                "[ConsumerA] User registered — UserId: {UserId}, Email: {Email}, Name: {Name}",
                message.UserId, message.Email, message.Name);
            return Task.CompletedTask;
        }, ct);
}
