using FluentValidation;
using GM.Mediator.Contracts;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events.Payments;
using GM.Messaging.Sample.Domain.SeedWork;

namespace GM.Messaging.Sample.Application.Payments.Commands;

public sealed record ProcessPaymentCommand(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Status) : IRequest<ProcessPaymentResult>;

public sealed record ProcessPaymentResult(Guid EventId, DateTime CreatedAtUtc);

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Status).NotEmpty();
    }
}

public class ProcessPaymentCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    public async Task<ProcessPaymentResult> Handle(
        ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        var evt = new PaymentProcessedIntegrationEvent(
            request.PaymentId,
            request.OrderId,
            request.Amount,
            request.Status);

        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(evt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProcessPaymentResult(evt.EventId, evt.OccurredAtUtc);
    }
}
