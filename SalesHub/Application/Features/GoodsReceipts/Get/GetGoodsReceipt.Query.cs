using System.Data;
using Application.Interfaces.Database;
using Application.Models.Common;
using MediatR;

namespace Application.Features.GoodsReceipts.Get;

public class GetGoodsReceiptQuery : IRequest<GetGoodsReceiptResponse>, ITransactionalRequest
{
    public Guid DocumentId {get;set;}

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
