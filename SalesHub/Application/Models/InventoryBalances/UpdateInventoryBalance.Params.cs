namespace Application.Models.InventoryBalances;

public class UpdateInventoryBalanceParams
{
    public int WarehouseId {get;set;}
    public int ProductId {get;set;}
    public int UnitId {get;set;}
    public int QuantityDelta {get;set;}    
    public decimal AmountDelta {get;set;}
}
