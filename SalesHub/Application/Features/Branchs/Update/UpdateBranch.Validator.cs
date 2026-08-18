using System.Data;
using Application.Features.Branchs.Update;
using FluentValidation;

namespace Application.Features.Branchs.Create;

public class UpdateBranchValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchValidator()
    {
        RuleFor(p => p.BranchId)
            .NotNull();
        
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
