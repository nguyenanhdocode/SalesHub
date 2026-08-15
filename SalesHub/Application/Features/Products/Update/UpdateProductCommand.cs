using System.Data;
using System.Reflection.Metadata;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Products.Update;

public class UpdateProductCommand : IRequest, ITransactionalRequest
{
    public int ProductId {get;set;}
    public string InternalCode {get;set;} = null!;
    public string ExternalCode {get;set;} = null!;
    public string Name {get;set;} = null!;
    public string CostingMethod {get;set;} = null!;
    public int BaseUnitId {get;set;}
    public bool Active {get;set;}
    public int SupplierId {get;set;}
    public List<int> UnitIds {get;set;} = [];
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
