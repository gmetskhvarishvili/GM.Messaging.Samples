using FluentValidation;

namespace GM.Messaging.Sample.API.Inventory;

public class UpdateInventoryRequestModel
{
    public Guid ProductId { get; set; }
    public string? Warehouse { get; set; }
    public int QuantityDelta { get; set; }
    public int NewQuantity { get; set; }
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
