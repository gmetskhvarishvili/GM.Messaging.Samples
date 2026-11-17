using FluentValidation;

namespace GM.Messaging.Sample.API.Returns;

public class RequestReturnRequestModel
{
    public required Guid ReturnId { get; set; }
    public required Guid OrderId { get; set; }
    public required Guid CustomerId { get; set; }
    public string? ContactEmail { get; set; }
    public string? Reason { get; set; }
}

public class RequestReturnRequestModelValidator : AbstractValidator<RequestReturnRequestModel>
{
    public RequestReturnRequestModelValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ContactEmail).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Reason).NotNull().NotEmpty().MaximumLength(500);
    }
}
