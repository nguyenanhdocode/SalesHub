namespace Application.Models.InventoryBalances;

public class InventoryBalanceParams
{
    public int WarehouseId {get;set;}
    public int ProductId {get;set;}
    public int UnitId {get;set;}
    public decimal Quantity {get;set;}    
    public decimal Amount {get;set;}
}
