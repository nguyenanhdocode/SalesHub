using MediatR;

namespace Application.Features.GoodsReceipts.Delete;

public class DeleteGoodsReceiptCommand : IRequest
{
    public Guid DocumentId {get;set;}
}
