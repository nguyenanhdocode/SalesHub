using FluentValidation;

namespace Application.Features.Warehouses.Update;

public class UpdateWarehouseValidator : AbstractValidator<UpdateWarehouseCommand>
{
    public UpdateWarehouseValidator()
    {
        RuleFor(p => p.Code)
            .NotNull()
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9._-]+$");

        RuleFor(p => p.Name)
            .NotNull()
            .NotEmpty()
            .MaximumLength(250);
    }
}
