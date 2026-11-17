using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;
using GM.Messaging.Sample.ConsumerB.Domain.SeedWork;
using GM.Messaging.Sample.Domain.Events.Inventory;

namespace GM.Messaging.Sample.ConsumerB.Worker.Handlers;

public class InventoryUpdatedHandler
{
    // InventoryUpdated is at version 2 (WarehouseCode); the producer's relay upcasts any version-1 rows.
    public static Task Handle(InventoryUpdatedIntegrationEvent message, IInboxProcessor inbox, IConsumerUnitOfWork unitOfWork, CancellationToken ct) =>
        inbox.ProcessAsync(message, async () =>
        {
            var stock = unitOfWork.StockLevelRepository;
            var level = await stock.FirstOrDefaultAsync(
                s => s.ProductId == message.ProductId && s.WarehouseCode == message.WarehouseCode, cancellationToken: ct);
            if (level is null)
                await stock.AddAsync(StockLevel.Create(message.ProductId, message.WarehouseCode, message.NewQuantity), ct);
            else
                level.Set(message.NewQuantity);
        }, ct);
}
