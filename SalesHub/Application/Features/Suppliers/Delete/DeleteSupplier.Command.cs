using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Suppliers.Delete;

public class DeleteSupplierCommand : IRequest, ITransactionalRequest
{
    public int SupplierId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
