using FluentValidation;
using GM.Mediator.Contracts;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events.Inventory;
using GM.Messaging.Sample.Domain.SeedWork;

namespace GM.Messaging.Sample.Application.Inventory.Commands;

public sealed record UpdateInventoryCommand(
    Guid ProductId,
    string Warehouse,
    int QuantityDelta,
    int NewQuantity) : IRequest<UpdateInventoryResult>;

public sealed record UpdateInventoryResult(Guid EventId, DateTime CreatedAtUtc);

public class UpdateInventoryCommandValidator : AbstractValidator<UpdateInventoryCommand>
{
    public UpdateInventoryCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Warehouse).NotEmpty();
        RuleFor(x => x.QuantityDelta).NotEqual(0);
    }
}

public class UpdateInventoryCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateInventoryCommand, UpdateInventoryResult>
{
    public async Task<UpdateInventoryResult> Handle(
        UpdateInventoryCommand request, CancellationToken cancellationToken)
    {
        var evt = new InventoryUpdatedIntegrationEvent(
            request.ProductId,
            request.Warehouse,
            request.QuantityDelta,
            request.NewQuantity);

        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(evt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateInventoryResult(evt.EventId, evt.OccurredAtUtc);
    }
}
