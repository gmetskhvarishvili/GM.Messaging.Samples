using GM.Messaging.Domain.Events;
using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace GM.Messaging.Sample.ConsumerB.Worker.Handlers;

/// <summary>
/// An erasure request (GM.Messaging's gm.subject-forgotten): the user's events are already unreadable, but this
/// service stored the address it sent the welcome email to, so it deletes that row. Idempotent: a repeat finds nothing.
/// </summary>
public class SubjectForgottenHandler
{
    public static Task Handle(SubjectForgottenIntegrationEvent message, IInboxProcessor inbox, ConsumerDbContext db, CancellationToken ct) =>
        inbox.ProcessAsync(message, async () =>
        {
            if (Guid.TryParse(message.Subject, out var userId))
                db.RemoveRange(await db.Set<WelcomeEmail>().Where(e => e.UserId == userId).ToListAsync(ct));
        }, ct);
}
