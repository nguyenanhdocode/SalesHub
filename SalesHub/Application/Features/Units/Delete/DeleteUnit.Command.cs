using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Units.Delete;

public class DeleteUnitCommand : IRequest, ITransactionalRequest
{
    public int UnitId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
