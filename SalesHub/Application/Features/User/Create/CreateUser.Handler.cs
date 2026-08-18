using Application.Database;
using Application.Exceptions;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.User.Create;

public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly DbSession _dbSession;
    private readonly ArgonPasswordHasher _passwordHasher;

    public CreateUserHandler(DbSession dbSession
        , ArgonPasswordHasher passwordHasher)
    {
        _dbSession = dbSession;
        _passwordHasher = passwordHasher;
    }

    private const string CHECK_EXISTS_QUERY = @"SELECT EXISTS(SELECT 1 FROM users WHERE username = @Username)";
    private const string INSERT_QUERY = @"
    INSERT INTO users (user_id, username, password)
    VALUES (@UserId, @UserName, @Password)
    ";

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbSession.Connection.QuerySingleAsync<bool>(CHECK_EXISTS_QUERY, new { request.UserName });

        if (user)
        {
            throw new BusinessException("exists");
        }

        var userId = Guid.CreateVersion7();

        await _dbSession.Connection.ExecuteAsync(INSERT_QUERY, new
        {
            UserId = userId,
            UserName = request.UserName,
            Password = _passwordHasher.Hash(request.Password)
        });

        return userId;
    }
}
