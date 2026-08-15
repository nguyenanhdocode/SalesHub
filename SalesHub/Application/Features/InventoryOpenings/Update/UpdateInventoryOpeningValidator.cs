using System.Data;
using FluentValidation;

namespace Application.Features.InventoryOpenings.Update;

public class UpdateInventoryOpeningValidator : AbstractValidator<UpdateInventoryOpeningCommand>
{
    public UpdateInventoryOpeningValidator()
    {
        RuleFor(p => p.Lines).NotEmpty();
        RuleFor(p => p.Note).MaximumLength(500);
        RuleForEach(p => p.Lines).SetValidator(new InventoryOpeningLineValidator());
    }
}
