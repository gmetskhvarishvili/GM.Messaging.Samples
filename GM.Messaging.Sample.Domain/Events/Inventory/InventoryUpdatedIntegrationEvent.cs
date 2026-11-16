using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Inventory;

/// <summary>
/// Stock changed. Version 2 renamed <c>Warehouse</c> to <see cref="WarehouseCode"/>; outbox rows
/// written at version 1 are converted by the upcaster in <see cref="SampleEventVersions"/>.
/// </summary>
[IntegrationEventName("inventory.inventory-updated", Version = 2)]
public sealed record InventoryUpdatedIntegrationEvent(
    Guid ProductId,
    string WarehouseCode,
    int QuantityDelta,
    int NewQuantity) : IntegrationEvent;
