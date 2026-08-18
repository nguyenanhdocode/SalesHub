namespace Application.Features.InventoryOpenings.Get;

public class GetInventoryOpeningLineResponse
{
    public int ProductId {get;set;}
    public string ProductInternalCode {get;set;} = null!;
    public string ProductName {get;set;} = null!;
    public int UnitId {get;set;}
    public string UnitCode {get;set;} = null!;
    public string UnitName {get;set;} = null!;
    public int Quantity {get;set;}
    public decimal Amount {get;set;}
    public int SortOrder {get;set;}
}
