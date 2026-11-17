using FluentValidation;
using GM.Mediator.Contracts;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events.Returns;
using GM.Messaging.Sample.Domain.SeedWork;

namespace GM.Messaging.Sample.Application.Returns.Commands;

public sealed record RequestReturnCommand(
    Guid ReturnId,
    Guid OrderId,
    Guid CustomerId,
    string ContactEmail,
    string Reason) : IRequest<RequestReturnResult>;

public sealed record RequestReturnResult(Guid EventId, DateTime CreatedAtUtc);

public class RequestReturnCommandValidator : AbstractValidator<RequestReturnCommand>
{
    public RequestReturnCommandValidator()
    {
        RuleFor(x => x.ReturnId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RequestReturnCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RequestReturnCommand, RequestReturnResult>
{
    public async Task<RequestReturnResult> Handle(RequestReturnCommand request, CancellationToken cancellationToken)
    {
        var evt = new ReturnRequestedIntegrationEvent(request.ReturnId, request.OrderId, request.CustomerId, request.ContactEmail, request.Reason);
        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(evt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RequestReturnResult(evt.EventId, evt.OccurredAtUtc);
    }
}
