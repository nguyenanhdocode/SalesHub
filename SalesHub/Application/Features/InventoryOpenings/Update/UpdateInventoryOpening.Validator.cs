using System.Data;
using FluentValidation;

namespace Application.Features.InventoryOpenings.Update;

public class UpdateInventoryOpeningValidator : AbstractValidator<UpdateInventoryOpeningCommand>
{
    public UpdateInventoryOpeningValidator()
    {
        RuleFor(p => p.Lines).NotEmpty();
        RuleFor(p => p.Note).MaximumLength(500);
        RuleFor(p => p.Lines)
            .NotEmpty()
            .Must(lines => !lines.GroupBy(p => new {p.ProductId, p.UnitId}).Any(g => g.Count() > 1));
        RuleForEach(p => p.Lines).SetValidator(new InventoryOpeningLineValidator());
    }
}
