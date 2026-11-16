using GM.Messaging.Domain.Events;
using GM.Messaging.Sample.Domain.Events.Inventory;

namespace GM.Messaging.Sample.Domain.Events;

/// <summary>
/// Upcasters for events whose payload shape changed. Register them once at startup in every process
/// that reads stored events (the producer worker's relay, and consumers' inbox workers).
/// </summary>
public static class SampleEventVersions
{
    private static int _registered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
            return;

        // InventoryUpdated v1 → v2: Warehouse was renamed WarehouseCode.
        IntegrationEventSerializer.AddUpcaster<InventoryUpdatedIntegrationEvent>(fromVersion: 1, v1 =>
        {
            v1["WarehouseCode"] = v1["Warehouse"]?.DeepClone();
            v1.Remove("Warehouse");
            return v1;
        });
    }
}
