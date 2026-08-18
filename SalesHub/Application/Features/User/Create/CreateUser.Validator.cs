using FluentValidation;

namespace Application.Features.User.Create;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(p => p.UserName)
            .MinimumLength(5)
            .MaximumLength(100)
            .Matches("^[a-zA-Z0-9@-_.@]+$");

         RuleFor(p => p.Password)
            .NotNull()
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&._#\-])[A-Za-z\d@$!%*?&._#\-]{8,}$");
    }
}
