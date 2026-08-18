using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Documents;

namespace Application.Features.GoodsReceipts.Create;

public class CreateGoodsReceiptCommand : CreateDocumentCommand, ITransactionalRequest
    , ICheckPeriodForCreateRequest
{
    public string? ShipperName {get;set;}
    public int WarehouseId {get;set;}
    public List<GoodsReceiptLineInput> Lines {get;set;} = [];

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
