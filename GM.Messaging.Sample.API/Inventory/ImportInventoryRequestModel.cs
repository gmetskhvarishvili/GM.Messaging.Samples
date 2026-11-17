using FluentValidation;

namespace GM.Messaging.Sample.API.Inventory;

public class ImportInventoryRequestModel
{
    public required Guid ImportId { get; set; }
    public string? Warehouse { get; set; }
    public List<ImportInventoryItem> Items { get; set; } = [];
}

public class ImportInventoryItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

public class ImportInventoryRequestModelValidator : AbstractValidator<ImportInventoryRequestModel>
{
    public ImportInventoryRequestModelValidator()
    {
        RuleFor(x => x.ImportId).NotEmpty();
        RuleFor(x => x.Warehouse).NotNull().NotEmpty().MaximumLength(50);
        RuleFor(x => x.Items).NotEmpty().Must(items => items.Count <= 20_000).WithMessage("At most 20000 items per import.");
    }
}
