using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Units.Get;

public class GetUnitQuery : IRequest<GetUnitResponse>, ITransactionalRequest
{
    public int UnitId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
