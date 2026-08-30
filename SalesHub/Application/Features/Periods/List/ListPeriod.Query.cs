using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Common;
using Application.Shared;
using MediatR;

namespace Application.Features.Periods.List;

public class ListPeriodQuery : IRequest<PagedResult<PeriodListItem>>, IPaginable, ITransactionalRequest
{
    public int? PeriodId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool? IsClosed { get; set; }
    public int PageNum { get; set; } = Constants.PAGE_NUM;
    public int PageSize { get; set; } = Constants.PAGE_SIZE;

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
