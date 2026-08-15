using Application.Features.Auth.Login;
using FluentValidation;

namespace Application.Features.User.Create;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(p => p.UserName)
            .NotNull()
            .NotEmpty();

         RuleFor(p => p.Password)
            .NotNull()
            .NotEmpty();
    }
}

