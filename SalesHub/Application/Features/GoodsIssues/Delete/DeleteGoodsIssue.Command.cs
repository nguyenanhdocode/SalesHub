using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.GoodsIssues.Delete;

public class DeleteGoodsIssueCommand : IRequest, ITransactionalRequest
    , ICheckPeriodForUpdateRequest
{
    public Guid DocumentId {get;set;}
    public string TableName => "documents";

    public string PkName => "document_id";

    public object PkValue => DocumentId;

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
