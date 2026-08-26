using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Common;
using Application.Shared;
using MediatR;

namespace Application.Features.Units.List;

public class ListUnitQuery : IRequest<PagedResult<UnitListItem>>, IPaginable, ITransactionalRequest
{
    public int? UnitId {get;set;}
    public string? Code {get;set;}
    public string? Name {get;set;}
    public bool? Active {get;set;}
    public int PageNum { get; set; } = Constants.PAGE_NUM;
    public int PageSize { get; set; } = Constants.PAGE_SIZE;
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
