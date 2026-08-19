using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Documents;
using MediatR;

namespace Application.Features.InventoryOpenings.Create;

public class CreateInventoryOpeningCommand : IRequest<CreateDocumentResponse>, ITransactionalRequest
    , ICheckPeriodForCreateRequest
{
    public int WarehouseId {get;set;}
    public int PeriodId {get;set;}
    public string? Note {get;set;}
    public List<InventoryOpeningLineInput> Lines {get;set;} = [];

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
