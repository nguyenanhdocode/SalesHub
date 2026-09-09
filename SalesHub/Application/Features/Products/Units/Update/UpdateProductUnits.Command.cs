using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Products.Units.Update;

public class UpdateProductUnitsCommand : IRequest, ITransactionalRequest
{
    public int ProductId {get;set;}
    public List<int> UnitIds {get;set;} = null!;
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
