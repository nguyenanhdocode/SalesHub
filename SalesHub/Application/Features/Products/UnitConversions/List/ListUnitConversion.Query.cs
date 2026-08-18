using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Products.UnitConversions.List;

public class ListUnitConversionsQuery : IRequest<IEnumerable<UnitConversionListItem>>, ITransactionalRequest
{
    public int ProductId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
