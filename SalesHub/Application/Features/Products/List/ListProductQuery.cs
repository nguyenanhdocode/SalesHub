using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Common;
using Application.Shared;
using MediatR;

namespace Application.Features.Products.List;

public class ListProductQuery : IRequest<PagedResult<ProductDto>>, IPaginable, ITransactionalRequest
{
    public int? ProductId {get;set;}
    public string? InternalCode {get;set;}
    public string? ExternalCode {get;set;}
    public string? Name {get;set;} = null!;
    // public string? CostingMethod {get;set;}
    public List<int>? BaseUnitIds {get;set;}
    public bool? Active {get;set;}
    public int? SupplierId {get;set;}
    public int PageNum { get; set; } = Constants.PAGE_NUM;
    public int PageSize { get; set; } = Constants.PAGE_SIZE;

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
