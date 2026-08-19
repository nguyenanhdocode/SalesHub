namespace Application.Models.InventoryBalances;

public class InventoryBalanceRow
{
    public int WarehouseId {get;set;}
    public int ProductId {get;set;}
    public int UnitId {get;set;}
    public int Quantity {get;set;}
    public decimal Amount {get;set;}
}
