using FluentValidation;

namespace Application.Features.Products.Create;

public class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(p => p.InternalCode)
            .NotNull()
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9@-_.-]+$");
        
        RuleFor(p => p.ExternalCode)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9@-_.-]+$");

        RuleFor(p => p.Name)
            .NotNull()
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(p => p.CostingMethod)
            .MaximumLength(10);
        
        RuleFor(p => p.BaseUnitId)
            .NotNull();

        RuleFor(p => p.SupplierId)
            .NotNull();
    }
}
