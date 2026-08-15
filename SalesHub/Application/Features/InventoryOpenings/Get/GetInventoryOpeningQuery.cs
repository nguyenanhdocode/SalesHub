using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.InventoryOpenings.Get;

public class GetInventoryOpeningQuery : IRequest<InventoryOpeningDto>, ITransactionalRequest
{
    public Guid DocumentId {get;set;}

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
