using FluentValidation;

namespace Application.Features.Units.Create;

public class CreateUnitValidator : AbstractValidator<CreateUnitCommand>
{
    public CreateUnitValidator()
    {
        RuleFor(p => p.Code)
            .NotNull()
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9._-]+$");

        RuleFor(p => p.Name)
            .NotNull()
            .NotEmpty()
            .MaximumLength(100);
    }
}
