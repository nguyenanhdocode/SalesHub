using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Documents;
using MediatR;

namespace Application.Features.GoodsIssues.Update;

public class UpdateGoodsIssueCommand : UpdateDocumentCommand, ITransactionalRequest
    , ICheckPeriodForUpdateRequest
{
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
    public string Reason {get;set;} = null!;
    public List<UpdateGoodsIssueLineInput> Lines {get;set;} = [];

    public string TableName => "documents";

    public string PkName => "document_id";

    public object PkValue => DocumentId;
}
