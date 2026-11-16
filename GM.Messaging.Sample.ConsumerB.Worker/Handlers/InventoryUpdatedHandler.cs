using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;
using GM.Messaging.Sample.Domain.Events.Inventory;
using Microsoft.EntityFrameworkCore;

namespace GM.Messaging.Sample.ConsumerB.Worker.Handlers;

public class InventoryUpdatedHandler
{
    // InventoryUpdated is at version 2 (WarehouseCode); the producer's relay upcasts any version-1 rows.
    public static Task Handle(InventoryUpdatedIntegrationEvent message, IInboxProcessor inbox, ConsumerDbContext db, CancellationToken ct) =>
        inbox.ProcessAsync(message, async () =>
        {
            var level = await db.Set<StockLevel>().FirstOrDefaultAsync(
                s => s.ProductId == message.ProductId && s.WarehouseCode == message.WarehouseCode, ct);
            if (level is null)
                db.Add(StockLevel.Create(message.ProductId, message.WarehouseCode, message.NewQuantity));
            else
                level.Set(message.NewQuantity);
        }, ct);
}
