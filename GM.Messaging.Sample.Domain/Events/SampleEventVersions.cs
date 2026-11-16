using System.Text.Json.Nodes;

namespace GM.Messaging.Sample.Domain.Events;

/// <summary>
/// Upcasters for events whose payload shape changed. Every process that reads these events registers
/// them with <c>AddGMIntegrationEvents</c>: the producer worker's relay (outbox rows written at an older
/// version) and the consumers (events published by a producer that is still on the older version,
/// recognised by the <c>gm-event-version</c> header).
/// </summary>
public static class SampleEventVersions
{
    /// <summary>InventoryUpdated v1 → v2: <c>Warehouse</c> was renamed <c>WarehouseCode</c>.</summary>
    public static JsonObject InventoryUpdatedV1ToV2(JsonObject v1)
    {
        ArgumentNullException.ThrowIfNull(v1);
        v1["WarehouseCode"] = v1["Warehouse"]?.DeepClone();
        v1.Remove("Warehouse");
        return v1;
    }
}
