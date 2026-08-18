using FluentValidation;

namespace Application.Features.Periods.Update;

public class UpdatePeriodValidator : AbstractValidator<UpdatePeriodCommand>
{
    public UpdatePeriodValidator()
    {
        RuleFor(p => p.Code)
            .NotNull()
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9@-_.@/]+$");

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
