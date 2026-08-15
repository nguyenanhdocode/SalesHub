using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Products.Create;

public class CreateProductCommand : IRequest<int>, ITransactionalRequest
{
    public string InternalCode {get;set;} = null!;
    public string? ExternalCode {get;set;} = null!;
    public string Name {get;set;} = null!;
    public string CostingMethod {get;set;} = null!;
    public int BaseUnitId {get;set;}
    public bool Active {get;set;}
    public int SupplierId {get;set;}

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
