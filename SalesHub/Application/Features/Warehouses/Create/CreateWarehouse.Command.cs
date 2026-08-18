using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Warehouses.Create;

public class CreateWarehouseCommand : IRequest<int>, ITransactionalRequest
{
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public int BranchId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
