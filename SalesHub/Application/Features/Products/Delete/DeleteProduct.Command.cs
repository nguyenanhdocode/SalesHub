using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Products.Delete;

public class DeleteProductCommand : IRequest, ITransactionalRequest
{
    public int ProductId {get;set;}

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
