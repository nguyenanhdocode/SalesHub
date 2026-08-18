using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Products.Get;

public class GetProductQuery : IRequest<GetProductResponse>, ITransactionalRequest
{
    public int ProductId {get;set;}

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
