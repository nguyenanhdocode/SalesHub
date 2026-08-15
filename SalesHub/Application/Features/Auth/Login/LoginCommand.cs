using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Auth.Login;

public class LoginCommand : IRequest<LoginResponse>, ITransactionalRequest
{
    public string UserName {get;set;} = null!;
    public string Password {get;set;} = null!;

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
