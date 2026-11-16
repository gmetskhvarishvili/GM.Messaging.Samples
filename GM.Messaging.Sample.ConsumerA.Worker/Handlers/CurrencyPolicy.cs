namespace GM.Messaging.Sample.ConsumerA.Worker.Handlers;

/// <summary>
/// Whether ConsumerA accepts orders in <see cref="OrderPlacedHandler.FailingCurrency"/>. It starts refusing them, so
/// such an order is retried and ends up in the broker's dead-letter queue; switching it on stands in for deploying a
/// fix (POST /admin/sample/accept-failing-currency), after which replaying the dead letter books the order.
/// </summary>
public sealed class CurrencyPolicy
{
    private volatile bool _acceptsFailingCurrency;

    public bool AcceptsFailingCurrency => _acceptsFailingCurrency;

    public void AcceptFailingCurrency() => _acceptsFailingCurrency = true;
}
