using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using GM.Messaging.Sample.Domain.Events.Customers;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

/// <summary>A scheduled reminder came due: recorded in the customer's activity feed.</summary>
public class ReminderDueHandler
{
    public static Task Handle(ReminderDueIntegrationEvent message, IInboxProcessor inbox, ConsumerDbContext db, CancellationToken ct) =>
        inbox.ProcessAsync(message, () =>
        {
            db.Add(CustomerActivity.Record(message.EventId, message.CustomerId, "reminder", message.Text, null, message.DueAtUtc));
            return Task.CompletedTask;
        }, ct);
}
