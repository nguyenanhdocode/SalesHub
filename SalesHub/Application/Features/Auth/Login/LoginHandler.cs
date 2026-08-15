using Application.Database;
using Application.Exceptions;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly DbSession _dbSession;
    private readonly ArgonPasswordHasher _passwordHasher;
    private readonly JwtProvider _jwtProvider;

    public LoginHandler(DbSession dbSession
        , ArgonPasswordHasher passwordHasher
        , JwtProvider jwtProvider)
    {
        _dbSession = dbSession;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
    }

    private const string GET_USER_BY_UNAME_QUERY = @"
        SELECT
            user_id AS UserId
            , username AS UserName
            , lock_until AS LockUntil
            , password AS Password
            , login_failed_count AS LoginFailedCount
            , activated AS Activated
        FROM users WHERE username = @UserName
    ";

    private const string INCREASE_FAILED_COUNT_QUERY = @"
        UPDATE users SET login_failed_count = login_failed_count + 1
        WHERE username = @UserName
    ";

    private const string LOCK_USER_QUERY = @"
        UPDATE users SET lock_until = @LockUntil, login_failed_count = 0
        WHERE username = @UserName
    ";

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbSession.Connection.QuerySingleOrDefaultAsync<UserDto>(GET_USER_BY_UNAME_QUERY, new
        {
            request.UserName
        });

        if (user == null)
        {
            throw new BusinessException("notfound");
        }

        if (user.LockUntil != null && user.LockUntil > DateTime.UtcNow)
        {
            throw new BusinessException("locked");
        }

        if (!await _passwordHasher.Verify(request.Password, user.Password))
        {
            if (user.LoginFailedCount > 5)
            {
                await _dbSession.Connection.ExecuteAsync(LOCK_USER_QUERY, new
                {
                    UserName = request.UserName,
                    LockUntil = DateTime.UtcNow.AddMinutes(5)
                });

            }
            else
            {
                await _dbSession.Connection.ExecuteAsync(INCREASE_FAILED_COUNT_QUERY, new
                {
                    request.UserName
                });
            }

            throw new BusinessException("notfound");
        }

        if (!user.Activated)
        {
            throw new BusinessException("not_activated");
        }

        // await _permissionService.LoadByUserId(user.UserId);

        return new LoginResponse
        {
            AccessToken = _jwtProvider.Generate(user.UserId, user.UserName)
        };
    }
}
