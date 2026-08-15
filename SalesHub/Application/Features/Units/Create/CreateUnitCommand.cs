using System.Data;
using Application.Interfaces.Database;
using Application.Models.Security;
using MediatR;

namespace Application.Features.Units.Create;

public class CreateUnitCommand : IRequest<CreateUnitResponse>, ITransactionalRequest
{
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
