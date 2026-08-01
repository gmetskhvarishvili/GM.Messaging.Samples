using FluentValidation;

namespace GM.Messaging.Sample.API.Users;

public class RegisterUserRequestModel
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
}

public class RegisterUserRequestModelValidator : AbstractValidator<RegisterUserRequestModel>
{
    public RegisterUserRequestModelValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Email).NotNull().NotEmpty().EmailAddress();
        RuleFor(x => x.Name).NotNull().NotEmpty();
    }
}
