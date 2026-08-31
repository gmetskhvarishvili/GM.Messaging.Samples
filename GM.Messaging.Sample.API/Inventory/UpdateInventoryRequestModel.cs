using FluentValidation;

namespace GM.Messaging.Sample.API.Inventory;

public class UpdateInventoryRequestModel
{
    public required Guid ProductId { get; set; }
    public string? Warehouse { get; set; }
    public required int QuantityDelta { get; set; }
    public required int NewQuantity { get; set; }
}

public class UpdateInventoryRequestModelValidator : AbstractValidator<UpdateInventoryRequestModel>
{
    public UpdateInventoryRequestModelValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Warehouse).NotNull().NotEmpty();
        RuleFor(x => x.QuantityDelta).NotEqual(0);
    }
}
