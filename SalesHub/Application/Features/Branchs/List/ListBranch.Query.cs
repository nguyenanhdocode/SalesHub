using System.Data;
using System.Reflection.Metadata;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Common;
using Application.Shared;
using MediatR;

namespace Application.Features.Branchs.List;

public class ListBranchQuery : IRequest<PagedResult<BranchListItem>>, IPaginable, ITransactionalRequest
{
    public int? BranchId {get;set;} = null!;
    public string? Code {get;set;} = null!;
    public string? Name {get;set;} = null!;
    public string? Address {get;set;}
    public string? Phone {get;set;}
    public string? Email {get;set;}
    public string? TaxCode {get;set;}
    public int PageNum { get; set; } = Constants.PAGE_NUM;
    public int PageSize { get; set; } = Constants.PAGE_SIZE;

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
