using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Documents;
using MediatR;

namespace Application.Features.InventoryOpenings.Create;

public class CreateInventoryOpeningCommand : IRequest<CreateDocumentResponse>, ITransactionalRequest
    , ICheckPeriodForCreateRequest
{
    public string? DocumentNo {get;set;}
    public int WarehouseId {get;set;}
    public int PeriodId {get;set;}
    public string? Note {get;set;}
    public bool WriteToBalances {get;set;}
    public List<CreateInventoryOpeningLineInput> Lines {get;set;} = [];

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
