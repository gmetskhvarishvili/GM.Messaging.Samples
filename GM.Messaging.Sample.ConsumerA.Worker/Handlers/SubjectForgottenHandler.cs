using GM.Messaging.Domain.Events;
using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.SeedWork;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

/// <summary>
/// An erasure request (GM.Messaging's gm.subject-forgotten): the user's events are already unreadable, but this
/// service copied their email into its activity feed, so it erases that copy too. Idempotent: a repeat finds nothing.
/// </summary>
public class SubjectForgottenHandler
{
    public static Task Handle(SubjectForgottenIntegrationEvent message, IInboxProcessor inbox, IConsumerUnitOfWork unitOfWork, CancellationToken ct) =>
        inbox.ProcessAsync(message, async () =>
        {
            if (!Guid.TryParse(message.Subject, out var customerId))
                return;
            foreach (var activity in await unitOfWork.CustomerActivityRepository.FindAsync(a => a.CustomerId == customerId, cancellationToken: ct))
                activity.ErasePersonalData();
        }, ct);
}
