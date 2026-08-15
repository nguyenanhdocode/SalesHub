using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Products.UnitConversions.Update;

public class UpdateUnitConversionsCommand : IRequest, ITransactionalRequest
{
    public int ProductId {get;set;}
    public List<UnitConversionDto> Conversions {get;set;} = [];

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
