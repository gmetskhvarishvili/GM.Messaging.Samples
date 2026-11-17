using FluentValidation;
using GM.Mediator.Contracts;
using GM.Messaging.Sample.Domain.BoundedContext.MessagingBoundedContext.OutboxMessageAggregate;
using GM.Messaging.Sample.Domain.Events.Inventory;
using GM.Messaging.Sample.Domain.SeedWork;

namespace GM.Messaging.Sample.Application.Inventory.Commands;

public sealed record ImportInventoryCommand(Guid ImportId, string Warehouse, IReadOnlyList<ImportedStock> Items) : IRequest<ImportInventoryResult>;

public sealed record ImportInventoryResult(Guid EventId, int Items);

public class ImportInventoryCommandValidator : AbstractValidator<ImportInventoryCommand>
{
    public ImportInventoryCommandValidator()
    {
        RuleFor(x => x.ImportId).NotEmpty();
        RuleFor(x => x.Warehouse).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Items).NotEmpty().Must(items => items.Count <= 20_000).WithMessage("At most 20000 items per import.");
    }
}

public class ImportInventoryCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ImportInventoryCommand, ImportInventoryResult>
{
    public async Task<ImportInventoryResult> Handle(ImportInventoryCommand request, CancellationToken cancellationToken)
    {
        var evt = new InventoryImportedIntegrationEvent(request.ImportId, request.Warehouse, request.Items);
        await unitOfWork.OutboxMessageRepository.AddAsync(OutboxMessage.From(evt), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ImportInventoryResult(evt.EventId, request.Items.Count);
    }
}
