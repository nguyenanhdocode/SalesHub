using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.GoodsReceipts.Delete;

public class DeleteGoodsReceiptCommand : IRequest, ITransactionalRequest
{
    public Guid DocumentId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
