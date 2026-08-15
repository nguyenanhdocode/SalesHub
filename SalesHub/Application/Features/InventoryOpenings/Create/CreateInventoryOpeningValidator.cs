using System.Data;
using FluentValidation;

namespace Application.Features.InventoryOpenings.Create;

public class CreateInventoryOpeningValidator : AbstractValidator<CreateInventoryOpeningCommand>
{
    public CreateInventoryOpeningValidator()
    {
        RuleFor(p => p.WarehouseId).NotNull();
        RuleFor(p => p.PeriodId).NotNull();
        RuleFor(p => p.Lines).NotEmpty();
        RuleForEach(p => p.Lines).SetValidator(new InventoryOpeningLineValidator());
    }
}
