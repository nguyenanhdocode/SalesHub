using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Documents;
using MediatR;

namespace Application.Features.GoodsIssues.Create;

public class CreateGoodsIssueCommand : CreateDocumentCommand, ITransactionalRequest
    , ICheckPeriodForCreateRequest
{
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;

    public int WarehouseId {get;set;}
    public string Reason {get;set;} = null!;
    public List<CreateGoodsIssueLineInput> Lines {get;set;} = [];
}
