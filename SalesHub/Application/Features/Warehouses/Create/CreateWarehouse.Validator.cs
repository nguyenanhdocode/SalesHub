using FluentValidation;

namespace Application.Features.Warehouses.Create;

public class CreateWarehouseValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseValidator()
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

        RuleFor(p => p.BranchId)
            .NotNull()
            .GreaterThan(0);
    }
}
