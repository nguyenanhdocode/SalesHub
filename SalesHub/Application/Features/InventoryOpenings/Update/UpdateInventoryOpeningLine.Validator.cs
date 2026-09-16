using FluentValidation;

namespace Application.Features.InventoryOpenings.Update;

public class UpdateInventoryOpeningLineValidator : AbstractValidator<UpdateInventoryOpeningLineInput>
{
    public UpdateInventoryOpeningLineValidator()
    {
        RuleFor(p => p.ProductId).NotNull();
        RuleFor(p => p.UnitId).NotNull();
        RuleFor(p => p.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(p => p.Amount).GreaterThanOrEqualTo(0);
    }
}
