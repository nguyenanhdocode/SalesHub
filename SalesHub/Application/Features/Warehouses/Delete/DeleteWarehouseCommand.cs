using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Warehouses.Delete;

public class DeleteWarehouseCommand : IRequest, ITransactionalRequest
{
    public int WarehouseId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
