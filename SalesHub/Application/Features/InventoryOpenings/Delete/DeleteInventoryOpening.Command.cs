using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.InventoryOpenings.Delete;

public class DeleteInventoryOpeningCommand : IRequest, ICheckPeriodForUpdateRequest
    , ITransactionalRequest
{
    public Guid DocumentId {get;set;}

    public string TableName => "inventory_openings";

    public string PkName => "document_id";

    public object PkValue => DocumentId;

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
