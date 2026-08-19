using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.InventoryOpenings.CarryForward;

public class CarryForwardCommand : IRequest, ITransactionalRequest
{
    public IList<int> WarehouseIds {get;set;} = [];
    public int SrcPeriodId {get;set;}
    public int DstPeriodId {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
