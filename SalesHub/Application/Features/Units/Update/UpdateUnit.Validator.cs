using FluentValidation;

namespace Application.Features.Units.Create;

public class UpdateUnitValidator : AbstractValidator<UpdateUnitCommand>
{
    public UpdateUnitValidator()
    {
        RuleFor(p => p.Code)
            .NotNull()
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Za-z0-9./_-]+$");

        RuleFor(p => p.Name)
            .NotNull()
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(p => p.Active).NotNull();
    }
}
