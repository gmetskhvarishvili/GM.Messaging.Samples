using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Domain.Events.Inventory;

public sealed record InventoryUpdatedIntegrationEvent(
    Guid ProductId,
    string Warehouse,
    int QuantityDelta,
    int NewQuantity) : IntegrationEvent;
