using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Branchs.Delete;

public class DeleteBranchCommand : IRequest, ITransactionalRequest
{
    public int BranchId {get;set;}

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
