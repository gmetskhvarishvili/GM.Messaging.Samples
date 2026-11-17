using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;
using GM.Messaging.Sample.ConsumerA.Domain.SeedWork;
using GM.Messaging.Sample.Domain.Events.Users;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

public class UserRegisteredHandler
{
    public static Task Handle(UserRegisteredIntegrationEvent message, IInboxProcessor inbox, IConsumerUnitOfWork unitOfWork, CancellationToken ct) =>
        inbox.ProcessAsync(message, () => unitOfWork.CustomerActivityRepository.AddAsync(
            CustomerActivity.Record(message.EventId, message.UserId, "user-registered", message.Email, null, message.OccurredAtUtc), ct), ct);
}
