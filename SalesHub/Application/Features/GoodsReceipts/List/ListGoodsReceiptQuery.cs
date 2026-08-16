using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Common;
using Application.Shared;
using MediatR;

namespace Application.Features.GoodsReceipts.List;

public class ListGoodsReceiptsQuery : IRequest<PagedResult<GoodsReceiptsDto>>, ITransactionalRequest
    , IPaginable
{
    public string? DocumentNo {get;set;}
    public List<int> PeriodIds {get;set;} = [];
    public DateTime? FromDate {get;set;}
    public DateTime? ToDate {get;set;}
    public string? CreatedBy {get;set;}
    public string? ShipperName {get;set;}
    public List<int> WarehouseIds {get;set;} = [];
    public List<int> BranchIds {get;set;} = [];
    public bool FilterByPeriod {get;set;}

    public IsolationLevel IsolationLevel;

    public int PageNum { get; set; } = Constants.PAGE_NUM;
    public int PageSize { get; set; } = Constants.PAGE_SIZE;

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
