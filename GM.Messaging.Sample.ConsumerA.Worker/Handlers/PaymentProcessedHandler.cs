using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using GM.Messaging.Sample.Domain.Events.Payments;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

public class PaymentProcessedHandler
{
    public static Task Handle(PaymentProcessedIntegrationEvent message, IInboxProcessor inbox, ConsumerDbContext db, CancellationToken ct) =>
        inbox.ProcessAsync(message, () =>
        {
            db.Add(CustomerActivity.Record(message.EventId, message.UserId, $"payment-{message.Status.ToLowerInvariant()}", message.PaymentId.ToString(), message.Amount, message.OccurredAtUtc));
            return Task.CompletedTask;
        }, ct);
}
