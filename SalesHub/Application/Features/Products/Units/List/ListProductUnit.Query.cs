using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Products.Units.List;

public class ListProductUnitQuery : IRequest<IEnumerable<ProductUnitListItem>>
    , ITransactionalRequest
{
    public int ProductId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
