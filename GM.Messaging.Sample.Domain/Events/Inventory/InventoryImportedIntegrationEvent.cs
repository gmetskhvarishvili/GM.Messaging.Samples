using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Inventory;

/// <summary>
/// A bulk stock import: every item's quantity in one warehouse. Thousands of items make it larger than
/// Messaging:LargeMessages:OffloadAboveBytes, so its body goes through the claim check (a shared directory) and only a
/// reference crosses RabbitMQ. ConsumerB sets the stock levels.
/// </summary>
[IntegrationEventName("inventory.inventory-imported")]
public sealed record InventoryImportedIntegrationEvent(Guid ImportId, string WarehouseCode, IReadOnlyList<ImportedStock> Items) : IntegrationEvent;

public sealed record ImportedStock(Guid ProductId, int Quantity);
