using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.Domain.Events.Inventory;
using Microsoft.Extensions.Logging;

namespace GM.Messaging.Sample.ConsumerB.Worker.Handlers;

public class InventoryUpdatedHandler(IInboxProcessor inbox, ILogger<InventoryUpdatedHandler> logger)
{
    public Task Handle(InventoryUpdatedIntegrationEvent message, CancellationToken ct) =>
        inbox.ProcessAsync(message, "ConsumerB", () =>
        {
            logger.LogInformation(
                "[ConsumerB] Inventory updated — ProductId: {ProductId}, Warehouse: {Warehouse}, Delta: {Delta}, New Qty: {NewQty}",
                message.ProductId, message.Warehouse, message.QuantityDelta, message.NewQuantity);
            return Task.CompletedTask;
        }, ct);
}
