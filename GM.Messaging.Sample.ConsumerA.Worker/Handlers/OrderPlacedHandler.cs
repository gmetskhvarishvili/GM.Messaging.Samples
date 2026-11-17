using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;
using GM.Messaging.Sample.ConsumerA.Domain.SeedWork;
using GM.Messaging.Sample.Domain.Events.Orders;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

public class OrderPlacedHandler
{
    /// <summary>The currency the sample treats as unprocessable until the "fix" is on (<see cref="CurrencyPolicy"/>), to show retries, dead letters and replay.</summary>
    public const string FailingCurrency = "XXX";

    // The unit of work's repositories share the inbox's ConsumerDbContext (Wolverine takes it from the message's
    // scope), so the activity and the inbox row commit together, or not at all. ProcessAsync saves: no SaveChanges here.
    public static Task Handle(OrderPlacedIntegrationEvent message, IInboxProcessor inbox, IConsumerUnitOfWork unitOfWork, CurrencyPolicy currencies, CancellationToken ct) =>
        inbox.ProcessAsync(message, async () =>
        {
            if (message.Currency == FailingCurrency && !currencies.AcceptsFailingCurrency)
                throw new InvalidOperationException($"ConsumerA can't book orders in {FailingCurrency}; Wolverine retries the message and the inbox row records each attempt.");

            await unitOfWork.CustomerActivityRepository.AddAsync(
                CustomerActivity.Record(message.EventId, message.CustomerId, "order-placed", message.OrderId.ToString(), message.TotalAmount, message.OccurredAtUtc), ct);
        }, ct);
}
