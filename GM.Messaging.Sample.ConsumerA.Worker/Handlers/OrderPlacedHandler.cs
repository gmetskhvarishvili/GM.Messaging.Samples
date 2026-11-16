using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;
using GM.Messaging.Sample.ConsumerA.Persistence.Context;
using GM.Messaging.Sample.Domain.Events.Orders;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

public class OrderPlacedHandler
{
    /// <summary>The currency the sample treats as unprocessable until the "fix" is on (<see cref="CurrencyPolicy"/>), to show retries, dead letters and replay.</summary>
    public const string FailingCurrency = "XXX";

    // ConsumerDbContext is the inbox's DbContext (Program.cs has Wolverine take both from the message's
    // scope), so the activity and the inbox row commit together, or not at all.
    public static Task Handle(OrderPlacedIntegrationEvent message, IInboxProcessor inbox, ConsumerDbContext db, CurrencyPolicy currencies, CancellationToken ct) =>
        inbox.ProcessAsync(message, () =>
        {
            if (message.Currency == FailingCurrency && !currencies.AcceptsFailingCurrency)
                throw new InvalidOperationException($"ConsumerA can't book orders in {FailingCurrency}; Wolverine retries the message and the inbox row records each attempt.");

            db.Add(CustomerActivity.Record(message.EventId, message.CustomerId, "order-placed", message.OrderId.ToString(), message.TotalAmount, message.OccurredAtUtc));
            return Task.CompletedTask;
        }, ct);
}
