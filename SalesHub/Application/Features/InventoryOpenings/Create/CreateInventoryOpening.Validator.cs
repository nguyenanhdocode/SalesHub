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
        RuleFor(p => p.Lines)
            .NotEmpty()
            .Must(lines => !lines.GroupBy(p => new {p.ProductId, p.UnitId}).Any(g => g.Count() > 1));
        RuleForEach(p => p.Lines).SetValidator(new InventoryOpeningLineValidator());
    }
}
