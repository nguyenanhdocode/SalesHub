using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Warehouses.Get;

public class GetWarehouseQuery : IRequest<WarehouseDto>, ITransactionalRequest
{
    public int WarehouseId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
