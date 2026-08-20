using System.Data;
using Application.Interfaces.Database;
using Application.Models.Common;
using MediatR;

namespace Application.Features.GoodsIssues.Get;

public class GetGoodsIssueQuery : IRequest<GetGoodsIssueResponse>, ITransactionalRequest
{
    public Guid DocumentId {get;set;}

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
