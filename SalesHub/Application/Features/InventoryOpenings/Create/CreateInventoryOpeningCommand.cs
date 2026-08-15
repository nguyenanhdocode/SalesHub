using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.InventoryOpenings.Create;

public class CreateInventoryOpeningCommand : IRequest<CreateInventoryOpeningResponse>, ITransactionalRequest
    , ICheckPeriodForCreateRequest
{
    public int WarehouseId {get;set;}
    public int PeriodId {get;set;}
    public string? Note {get;set;}
    public List<InventoryOpeningLineDto> Lines {get;set;} = [];

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
