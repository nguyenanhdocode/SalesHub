namespace Application.Features.Products.Units.List;

public class ProductUnitListItem
{
    public int UnitId {get;set;}
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public bool IsBaseUnit {get;set;}
}
