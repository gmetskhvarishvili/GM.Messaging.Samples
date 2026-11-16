using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using GM.Messaging.Sample.Domain.Events.Users;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

public class UserRegisteredHandler
{
    public static Task Handle(UserRegisteredIntegrationEvent message, IInboxProcessor inbox, ConsumerDbContext db, CancellationToken ct) =>
        inbox.ProcessAsync(message, () =>
        {
            db.Add(CustomerActivity.Record(message.EventId, message.UserId, "user-registered", message.Email, null, message.OccurredAtUtc));
            return Task.CompletedTask;
        }, ct);
}
