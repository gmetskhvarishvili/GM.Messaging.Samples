using GM.EntityFramework.Domain.Abstractions;

namespace GM.Messaging.Sample.ConsumerA.Domain.BoundedContext.ActivityBoundedContext;

/// <summary>
/// One thing ConsumerA learned about a customer from an integration event. Saved through the same
/// DbContext as the inbox row, so it exists exactly when the event counts as handled.
/// </summary>
public sealed class CustomerActivity : IAggregateRoot
{
    private CustomerActivity() { }

    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>The integration event this activity came from (unique: one activity per event).</summary>
    public Guid EventId { get; private set; }

    public Guid? CustomerId { get; private set; }
    public string Kind { get; private set; } = default!;
    public string Reference { get; private set; } = default!;
    public decimal? Amount { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    public static CustomerActivity Record(Guid eventId, Guid? customerId, string kind, string reference, decimal? amount, DateTime occurredAtUtc) =>
        new()
        {
            EventId = eventId,
            CustomerId = customerId,
            Kind = kind,
            Reference = reference,
            Amount = amount,
            OccurredAtUtc = occurredAtUtc,
        };
}
