using System.Diagnostics;
using System.Text.Json;
using GM.Messaging.Domain.Events;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events;
using GM.Messaging.Sample.Domain.Events.Inventory;
using GM.Messaging.Sample.Domain.Events.Orders;
using GM.Messaging.Sample.Domain.Events.Payments;
using GM.Messaging.Sample.Domain.Events.Returns;
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
    public void From_UserRegistered_KeepsTheEmailAndNameOutOfThePayload()
    {
        var outbox = OutboxMessage.From(new UserRegisteredIntegrationEvent(Guid.NewGuid(), "ana@example.test", "Ana Beridze"));

        Assert.DoesNotContain("ana@example.test", outbox.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain("Beridze", outbox.Payload, StringComparison.Ordinal);
        Assert.Contains("gmenc:", outbox.Payload, StringComparison.Ordinal);
        var read = (UserRegisteredIntegrationEvent)IntegrationEventSerializer.Deserialize(outbox.Payload, typeof(UserRegisteredIntegrationEvent), outbox.EventVersion);
        Assert.Equal(("ana@example.test", "Ana Beridze"), (read.Email, read.Name));
    }

    [Fact]
    public void From_UserRegistered_EncryptsWithTheUsersOwnKey()
    {
        var userId = Guid.NewGuid();
        var outbox = OutboxMessage.From(new UserRegisteredIntegrationEvent(userId, "ana@example.test", "Ana"));

        // UserId (inherited, named by [DataSubject(nameof(UserId))]) stays readable, once, and names whose key is used.
        Assert.Single(JsonElement.Parse(outbox.Payload).EnumerateObject(), p => p.Name.Equals("UserId", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(userId.ToString(), TestSensitiveDataProtector.Subjects);
        var read = (UserRegisteredIntegrationEvent)IntegrationEventSerializer.Deserialize(outbox.Payload, typeof(UserRegisteredIntegrationEvent), outbox.EventVersion);
        Assert.Equal(userId, read.UserId);
        Assert.Equal(userId, ((IntegrationEvent)read).UserId);
    }

    [Fact]
    public void From_ReturnRequested_EncryptsTheContactEmailWithTheCustomersKey()
    {
        var customerId = Guid.NewGuid();
        var outbox = OutboxMessage.From(new ReturnRequestedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), customerId, "ana@example.test", "Wrong size"));

        Assert.Equal("returns.return-requested", outbox.EventType);
        Assert.DoesNotContain("ana@example.test", outbox.Payload, StringComparison.Ordinal);
        Assert.Contains("Wrong size", outbox.Payload, StringComparison.Ordinal);
        Assert.Contains(customerId.ToString(), TestSensitiveDataProtector.Subjects);
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
        IntegrationEventSerializer.AddUpcaster<InventoryUpdatedIntegrationEvent>(1, SampleEventVersions.InventoryUpdatedV1ToV2);

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
