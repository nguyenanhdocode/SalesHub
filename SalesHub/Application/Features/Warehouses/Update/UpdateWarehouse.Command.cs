using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Warehouses.Update;

public class UpdateWarehouseCommand : IRequest, ITransactionalRequest
{
    public int WarehouseId {get;set;}
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public bool Active {get;set;}
    public int BranchId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
