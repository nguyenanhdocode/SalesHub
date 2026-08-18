namespace Application.Features.Warehouses.List;

public class WarehouseListItem
{
    public int WarehouseId {get;set;}
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public bool Active {get;set;}
    public DateTime CreatedAt {get;set;}
    public DateTime? UpdatedAt {get;set;}
    public int BranchId {get;set;}
    public string BranchCode {get;set;} = null!;
    public string BranchName {get;set;} = null!;
}
