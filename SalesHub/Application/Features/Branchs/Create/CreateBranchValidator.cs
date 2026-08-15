using System.Data;
using FluentValidation;

namespace Application.Features.Branchs.Create;

public class CreateBranchValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchValidator()
    {
        RuleFor(p => p.Code)
            .NotNull()
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-zA-Z0-9@-_.@]+$");

        RuleFor(p => p.Name)
            .NotNull()
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(p => p.Address)
            .MaximumLength(250);

        RuleFor(p => p.Phone)
            .MaximumLength(50);

        RuleFor(p => p.Email)
            .MaximumLength(250);

        RuleFor(p => p.TaxCode)
            .MaximumLength(50);
    }
}
