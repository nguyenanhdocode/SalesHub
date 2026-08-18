using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Branchs.Get;

public class GetBranchQuery : IRequest<GetBranchResponse>, ITransactionalRequest
{
    public int BranchId {get;set;}

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
