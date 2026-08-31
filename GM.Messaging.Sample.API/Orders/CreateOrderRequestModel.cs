using FluentValidation;

namespace GM.Messaging.Sample.API.Orders;

public class CreateOrderRequestModel
{
    public required Guid OrderId { get; set; }
    public required Guid CustomerId { get; set; }
    public required decimal TotalAmount { get; set; }
    public string? Currency { get; set; }
}

public class CreateOrderRequestModelValidator : AbstractValidator<CreateOrderRequestModel>
{
    public CreateOrderRequestModelValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.Currency).NotNull().NotEmpty().Length(3);
    }
}
