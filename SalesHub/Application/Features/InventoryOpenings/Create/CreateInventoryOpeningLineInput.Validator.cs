using FluentValidation;

namespace Application.Features.InventoryOpenings.Create;

public class CreateInventoryOpeningLineValidator : AbstractValidator<CreateInventoryOpeningLineInput>
{
    public CreateInventoryOpeningLineValidator()
    {
        RuleFor(p => p.ProductId).NotNull();
        RuleFor(p => p.UnitId).NotNull();
        RuleFor(p => p.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(p => p.Amount).GreaterThanOrEqualTo(0);
    }
}
