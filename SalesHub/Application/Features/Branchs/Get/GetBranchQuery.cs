using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Branchs.Get;

public class GetBranchQuery : IRequest<BranchDto>, ITransactionalRequest
{
    public int BranchId {get;set;}

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
