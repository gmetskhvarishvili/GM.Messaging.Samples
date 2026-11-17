using GM.Messaging.Domain.Events;
using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

/// <summary>
/// An erasure request (GM.Messaging's gm.subject-forgotten): the user's events are already unreadable, but this
/// service copied their email into its activity feed, so it erases that copy too. Idempotent: a repeat finds nothing.
/// </summary>
public class SubjectForgottenHandler
{
    public static Task Handle(SubjectForgottenIntegrationEvent message, IInboxProcessor inbox, ConsumerDbContext db, CancellationToken ct) =>
        inbox.ProcessAsync(message, async () =>
        {
            if (!Guid.TryParse(message.Subject, out var customerId))
                return;
            foreach (var activity in await db.Set<CustomerActivity>().Where(a => a.CustomerId == customerId).ToListAsync(ct))
                activity.ErasePersonalData();
        }, ct);
}
