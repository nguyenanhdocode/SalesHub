using System.Data;
using Application.Interfaces.Database;
using Application.Models.Security;
using MediatR;

namespace Application.Features.User.Create;

public sealed class CreateUserCommand : IRequest<Guid>, ITransactionalRequest
{
    public string UserName {get;set;} = null!;
    public string Password {get;set;} = null!;
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
