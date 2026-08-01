using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.Domain.Events.Orders;
using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

public class OrderPlacedHandler(IInboxProcessor inbox, ILogger<OrderPlacedHandler> logger)
{
    public Task Handle(OrderPlacedIntegrationEvent message, CancellationToken ct) =>
        inbox.ProcessAsync(message, "ConsumerA", () =>
        {
            logger.LogInformation(
                "[ConsumerA] Order placed — OrderId: {OrderId}, Customer: {CustomerId}, Total: {Total} {Currency}",
                message.OrderId, message.CustomerId, message.TotalAmount, message.Currency);
            return Task.CompletedTask;
        }, ct);
}
