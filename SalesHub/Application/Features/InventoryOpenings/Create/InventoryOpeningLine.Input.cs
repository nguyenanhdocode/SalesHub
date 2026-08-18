namespace Application.Features.InventoryOpenings.Create;

public class InventoryOpeningLineInput
{
    public int ProductId {get;set;}
    public int UnitId {get;set;}
    public int Quantity {get;set;}
    public decimal Amount {get;set;}
    public int SortOrder {get;set;}
}
