using FluentValidation;

namespace Application.Features.Periods.Create;

public class CreatePeriodValidator : AbstractValidator<CreatePeriodCommand>
{
    public CreatePeriodValidator()
    {
        RuleFor(p => p.Code)
            .NotNull()
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9@-_-./]+$");

        RuleFor(p => p.Name)
            .NotNull()
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(p => p.FromDate)
            .NotNull();

        RuleFor(p => p.ToDate)
            .NotNull()
            .GreaterThan(p => p.FromDate);
    }
}
