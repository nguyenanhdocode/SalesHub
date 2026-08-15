using FluentValidation;

namespace Application.Features.Suppliers.Create;

public class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierValidator()
    {
        RuleFor(p => p.Code).NotNull().NotEmpty().MaximumLength(250).Matches("^[a-zA-Z0-9@-_.@]+$");
        RuleFor(p => p.Name).NotNull().NotEmpty().MaximumLength(500);
        RuleFor(p => p.ContactPerson).MaximumLength(50);
        RuleFor(p => p.Phone).MaximumLength(50);
        RuleFor(p => p.TaxCode).MaximumLength(50);
        RuleFor(p => p.Email).MaximumLength(255);
        RuleFor(p => p.Address).MaximumLength(255);
    }
}
