using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Documents;

namespace Application.Features.GoodsReceipts.Update;

public class UpdateGoodsReceiptCommand : UpdateDocumentCommand, ITransactionalRequest
    , ICheckPeriodForUpdateRequest
{
    public string? ShipperName {get;set;}
    public List<GoodsReceiptLineInput> Lines {get;set;} = [];

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    public string TableName => "documents";

    public string PkName => "document_id";

    public object PkValue => DocumentId;
}
