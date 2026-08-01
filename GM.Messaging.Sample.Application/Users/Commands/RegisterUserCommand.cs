using FluentValidation;
using GM.Mediator.Contracts;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events.Users;
using GM.Messaging.Sample.Domain.SeedWork;

namespace GM.Messaging.Sample.Application.Users.Commands;

public sealed record RegisterUserCommand(
    Guid UserId,
    string Email,
    string Name) : IRequest<RegisterUserResult>;

public sealed record RegisterUserResult(Guid EventId, DateTime CreatedAtUtc);

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Name).NotEmpty();
    }
}

public class RegisterUserCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterUserCommand, RegisterUserResult>
{
    public async Task<RegisterUserResult> Handle(
        RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var evt = new UserRegisteredIntegrationEvent(
            request.UserId,
            request.Email,
            request.Name);

        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(evt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterUserResult(evt.EventId, evt.OccurredAtUtc);
    }
}
