using FluentValidation;

namespace GM.Messaging.Sample.API.Payments;

public class ProcessPaymentRequestModel
{
    public required Guid PaymentId { get; set; }
    public required Guid OrderId { get; set; }
    public required decimal Amount { get; set; }
    public string? Status { get; set; }
}

public class ProcessPaymentRequestModelValidator : AbstractValidator<ProcessPaymentRequestModel>
{
    public ProcessPaymentRequestModelValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Status).NotNull().NotEmpty();
    }
}
