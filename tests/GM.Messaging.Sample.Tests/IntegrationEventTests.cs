using GM.Messaging.Sample.Domain.Events.Inventory;
using GM.Messaging.Sample.Domain.Events.Orders;
using GM.Messaging.Sample.Domain.Events.Payments;
using GM.Messaging.Sample.Domain.Events.Users;
using Xunit;

namespace GM.Messaging.Sample.Tests;

public class IntegrationEventTests
{
    [Fact]
    public void OrderPlaced_gets_an_id_and_timestamp_and_keeps_its_payload()
    {
        var evt = new OrderPlacedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), 10m, "USD");

        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.NotEqual(default, evt.OccurredAtUtc);
        Assert.Equal("USD", evt.Currency);
    }

    [Fact]
    public void The_other_events_carry_their_payloads()
    {
        var payment = new PaymentProcessedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), 10m, "Captured");
        Assert.Equal("Captured", payment.Status);

        var inventory = new InventoryUpdatedIntegrationEvent(Guid.NewGuid(), "WH-1", -2, 8);
        Assert.Equal(8, inventory.NewQuantity);

        var user = new UserRegisteredIntegrationEvent(Guid.NewGuid(), "a@b.com", "Ada");
        Assert.Equal("a@b.com", user.Email);
        Assert.NotNull(user.UserId);
    }
}
