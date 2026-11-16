using GM.EntityFramework.Domain.Abstractions;

namespace GM.Messaging.Sample.ConsumerB.Domain.BoundedContext.FulfilmentBoundedContext;

/// <summary>
/// An order's history as ConsumerB saw it. Order and payment events share the order's ordering key and
/// a sequential queue, so entries appear in the order the producer raised them.
/// </summary>
public sealed class OrderTimelineEntry : IAggregateRoot
{
    private OrderTimelineEntry() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EventId { get; private set; }
    public Guid OrderId { get; private set; }
    public string Kind { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public DateTime RaisedAtUtc { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    public static OrderTimelineEntry Record(Guid eventId, Guid orderId, string kind, decimal amount, DateTime raisedAtUtc) =>
        new() { EventId = eventId, OrderId = orderId, Kind = kind, Amount = amount, RaisedAtUtc = raisedAtUtc, RecordedAtUtc = DateTime.UtcNow };
}

/// <summary>Stock per product and warehouse, kept from InventoryUpdated events.</summary>
public sealed class StockLevel : IAggregateRoot
{
    private StockLevel() { }

    public Guid ProductId { get; private set; }
    public string WarehouseCode { get; private set; } = default!;
    public int Quantity { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static StockLevel Create(Guid productId, string warehouseCode, int quantity) =>
        new() { ProductId = productId, WarehouseCode = warehouseCode, Quantity = quantity, UpdatedAtUtc = DateTime.UtcNow };

    public void Set(int quantity)
    {
        Quantity = quantity;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

/// <summary>A welcome email ConsumerB "sent" (recorded) for a registered user, through the inbox worker.</summary>
public sealed class WelcomeEmail : IAggregateRoot
{
    private WelcomeEmail() { }

    public Guid EventId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Email { get; private set; } = default!;
    public DateTime SentAtUtc { get; private set; }

    public static WelcomeEmail Sent(Guid eventId, Guid? userId, string email) =>
        new() { EventId = eventId, UserId = userId, Email = email, SentAtUtc = DateTime.UtcNow };
}
