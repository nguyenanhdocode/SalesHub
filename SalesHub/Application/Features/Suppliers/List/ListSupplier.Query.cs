using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Common;
using Application.Shared;
using MediatR;

namespace Application.Features.Suppliers.List;

public class ListSupplierQuery : IRequest<PagedResult<SupplierListItem>>, IPaginable, ITransactionalRequest
{
    public string? Code {get;set;}
    public string? Name {get;set;}
    public string? ContactPerson {get;set;}
    public string? Phone {get;set;}
    public string? TaxCode {get;set;}
    public string? Email {get;set;}
    public string? Address {get;set;}
    public int PageNum { get; set; } = Constants.PAGE_NUM;
    public int PageSize { get; set; } = Constants.PAGE_SIZE;
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
