using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;
using GM.Messaging.Sample.ConsumerB.Domain.SeedWork;
using GM.Messaging.Sample.Domain.Events.Orders;
using GM.Messaging.Sample.Domain.Events.Payments;

namespace GM.Messaging.Sample.ConsumerB.Worker.Handlers;

// Order and payment events arrive on one sequential queue (appsettings: "Sequential": true), and the
// producer's relay published them in order per order (their ordering key), so the timeline keeps it.
// (Wolverine discovers classes whose names end in "Handler" or "Consumer".)
public class OrderTimelineHandler
{
    public static Task Handle(OrderPlacedIntegrationEvent message, IInboxProcessor inbox, IConsumerUnitOfWork unitOfWork, CancellationToken ct) =>
        inbox.ProcessAsync(message, () => unitOfWork.OrderTimelineEntryRepository.AddAsync(
            OrderTimelineEntry.Record(message.EventId, message.OrderId, "placed", message.TotalAmount, message.OccurredAtUtc), ct), ct);

    public static Task Handle(PaymentProcessedIntegrationEvent message, IInboxProcessor inbox, IConsumerUnitOfWork unitOfWork, CancellationToken ct) =>
        inbox.ProcessAsync(message, () => unitOfWork.OrderTimelineEntryRepository.AddAsync(
            OrderTimelineEntry.Record(message.EventId, message.OrderId, $"payment-{message.Status.ToLowerInvariant()}", message.Amount, message.OccurredAtUtc), ct), ct);
}
