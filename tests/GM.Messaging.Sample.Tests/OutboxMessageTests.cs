using System.Diagnostics;
using System.Text.Json;
using GM.Messaging.Domain.Events;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events;
using GM.Messaging.Sample.Domain.Events.Inventory;
using GM.Messaging.Sample.Domain.Events.Orders;
using GM.Messaging.Sample.Domain.Events.Payments;
using GM.Messaging.Sample.Domain.Events.Users;
using Xunit;

namespace GM.Messaging.Sample.Tests;

public class OutboxMessageTests
{
    [Fact]
    public void From_StoresTheStableNameAndPayload()
    {
        var evt = new OrderPlacedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), 99.9m, "EUR");

        var outbox = OutboxMessage.From(evt);

        Assert.Equal("orders.order-placed", outbox.EventType);
        Assert.Equal(typeof(OrderPlacedIntegrationEvent), IntegrationEventTypes.Resolve(outbox.EventType));
        var roundtrip = JsonSerializer.Deserialize<OrderPlacedIntegrationEvent>(outbox.Payload);
        Assert.NotNull(roundtrip);
        Assert.Equal(("EUR", evt.OrderId), (roundtrip.Currency, roundtrip.OrderId));
    }

    [Fact]
    public void From_OrderAndPaymentEvents_ShareTheOrdersOrderingKey()
    {
        var orderId = Guid.NewGuid();

        var placed = OutboxMessage.From(new OrderPlacedIntegrationEvent(orderId, Guid.NewGuid(), 10m, "EUR"));
        var paid = OutboxMessage.From(new PaymentProcessedIntegrationEvent(Guid.NewGuid(), orderId, 10m, "Succeeded"));
        var registered = OutboxMessage.From(new UserRegisteredIntegrationEvent(Guid.NewGuid(), "a@b.test", "A"));

        Assert.Equal(orderId.ToString(), placed.OrderingKey);
        Assert.Equal(placed.OrderingKey, paid.OrderingKey);
        Assert.Null(registered.OrderingKey);
    }

    [Fact]
    public void InventoryUpdated_Version1Row_IsUpcastToWarehouseCode()
    {
        SampleEventVersions.Register();

        var evt = (InventoryUpdatedIntegrationEvent)IntegrationEventSerializer.Deserialize(
            """{"ProductId":"6b1d6c1e-3c39-4a7e-9a59-1a0f2a3b4c5d","Warehouse":"TBS-1","QuantityDelta":-2,"NewQuantity":8}""",
            typeof(InventoryUpdatedIntegrationEvent),
            storedVersion: 1);

        Assert.Equal("TBS-1", evt.WarehouseCode);
        Assert.Equal(2, OutboxMessage.From(evt).EventVersion);
    }

    [Fact]
    public void From_InsideARequestTrace_CapturesIt()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "sample-tests",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("sample-tests");
        using var request = source.StartActivity("request")!;

        var outbox = OutboxMessage.From(new UserRegisteredIntegrationEvent(Guid.NewGuid(), "a@b.test", "A"));

        Assert.Equal(request.Id, outbox.TraceParent);
    }
}
