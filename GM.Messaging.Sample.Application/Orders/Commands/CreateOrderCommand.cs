using FluentValidation;
using GM.Mediator.Contracts;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events.Orders;
using GM.Messaging.Sample.Domain.SeedWork;


namespace GM.Messaging.Sample.Application.Orders.Commands;

public sealed record CreateOrderCommand(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency) : IRequest<CreateOrderResult>;

public sealed record CreateOrderResult(Guid EventId, DateTime CreatedAtUtc);

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.TotalAmount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public class CreateOrderCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(
        CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var evt = new OrderPlacedIntegrationEvent(
            request.OrderId,
            request.CustomerId,
            request.TotalAmount,
            request.Currency);

        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(evt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateOrderResult(evt.EventId, evt.OccurredAtUtc);
    }
}
