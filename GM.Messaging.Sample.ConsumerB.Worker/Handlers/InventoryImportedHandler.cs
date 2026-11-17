using GM.Messaging.Persistence.Inbox;
using GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;
using GM.Messaging.Sample.ConsumerB.Persistence.Context;
using GM.Messaging.Sample.Domain.Events.Inventory;
using Microsoft.EntityFrameworkCore;

namespace GM.Messaging.Sample.ConsumerB.Worker.Handlers;

/// <summary>
/// A bulk stock import. The event is large, so it arrived through the claim check: Wolverine loaded its body from the
/// shared directory before this handler runs. One query loads the warehouse's existing levels.
/// </summary>
public class InventoryImportedHandler
{
    public static Task Handle(InventoryImportedIntegrationEvent message, IInboxProcessor inbox, ConsumerDbContext db, CancellationToken ct) =>
        inbox.ProcessAsync(message, async () =>
        {
            var existing = await db.Set<StockLevel>().Where(s => s.WarehouseCode == message.WarehouseCode)
                .ToDictionaryAsync(s => s.ProductId, ct);
            foreach (var item in message.Items)
            {
                if (existing.TryGetValue(item.ProductId, out var level))
                    level.Set(item.Quantity);
                else
                    db.Add(StockLevel.Create(item.ProductId, message.WarehouseCode, item.Quantity));
            }
        }, ct);
}
