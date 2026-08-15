using System.Data;
using Application.Interfaces.Database;
using Application.Models.Common;
using MediatR;

namespace Application.Features.Products.Get;

public class GetProductQuery : IRequest<ProductDto>, ITransactionalRequest
{
    public int ProductId {get;set;}

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
