using GM.Messaging.Sample.Application.Orders.Commands;
using Xunit;

namespace GM.Messaging.Sample.Tests;

public class ValidatorTests
{
    private static CreateOrderCommand ValidOrder() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 42.50m, "USD");

    [Fact]
    public void CreateOrder_accepts_a_valid_command()
    {
        Assert.True(new CreateOrderCommandValidator().Validate(ValidOrder()).IsValid);
    }

    [Fact]
    public void CreateOrder_rejects_empty_ids_zero_amount_and_bad_currency()
    {
        var result = new CreateOrderCommandValidator().Validate(
            new CreateOrderCommand(Guid.Empty, Guid.Empty, 0m, "US"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.OrderId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.TotalAmount));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderCommand.Currency));
    }
}
