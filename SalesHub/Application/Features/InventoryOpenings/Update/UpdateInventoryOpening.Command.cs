using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.InventoryOpenings.Update;

public class UpdateInventoryOpeningCommand : IRequest, ITransactionalRequest
    , ICheckPeriodForUpdateRequest
{
    public Guid DocumentId {get;set;}
    public string? Note {get;set;}
    public List<InventoryOpeningLineInput> Lines {get;set;} = [];

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    public string TableName => "inventory_openings";

    public string PkName => "document_id";

    public object PkValue => DocumentId;
}
