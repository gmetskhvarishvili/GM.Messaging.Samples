using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.Domain.Events.Users;

namespace GM.Messaging.Sample.ConsumerB.Worker.Handlers;

public class UserRegisteredHandler
{
    // Sending email is slow and can fail, so don't do it on the broker's delivery: store the event and
    // let the inbox worker send it (InboxHandlers/SendWelcomeEmail), with retries and dead-lettering.
    public static Task Handle(UserRegisteredIntegrationEvent message, IInboxProcessor inbox, CancellationToken ct) =>
        inbox.IngestAsync(message, ct);
}
