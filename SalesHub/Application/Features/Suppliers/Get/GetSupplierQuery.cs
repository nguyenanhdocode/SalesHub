using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Suppliers.Get;

public class GetSupplierQuery : IRequest<SupplierDto>, ITransactionalRequest
{
    public int SupplierId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
