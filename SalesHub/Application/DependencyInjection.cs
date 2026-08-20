using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using Application.Features.User.Create;
using Application.Behaviors;
using Application.Services;
using Application.Database;
using Infrastructure.Security;
using Application.Interfaces.Security;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services
        , IConfiguration configuration)
    {
        services.AddScoped<DbSession>();
        services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
        services.AddSingleton<ArgonPasswordHasher>();
        services.AddSingleton<JwtProvider>(p => new JwtProvider(configuration));
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddHttpContextAccessor();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);

            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
            cfg.AddOpenBehavior(typeof(PeriodBehavior<,>));
        });

        services.AddScoped<DocumentNoService>();

        return services;
    }
}
