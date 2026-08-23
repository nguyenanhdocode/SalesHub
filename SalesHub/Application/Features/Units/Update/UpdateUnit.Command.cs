using System.Data;
using Application.Interfaces.Database;
using Application.Models.Security;
using MediatR;

namespace Application.Features.Units.Create;

public class UpdateUnitCommand : IRequest, ITransactionalRequest
{
    public int UnitId {get;set;}
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public bool Active {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
