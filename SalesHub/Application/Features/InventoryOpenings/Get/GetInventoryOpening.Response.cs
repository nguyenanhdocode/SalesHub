namespace Application.Features.InventoryOpenings.Get;

public class GetInventoryOpeningResponse
{
    public Guid DocumentId {get;set;}
    public string DocumentNo {get;set;} = null!;
    public int BranchId {get;set;}
    public string BranchCode {get;set;} = null!;
    public string BranchName {get;set;} = null!;
    public int WarehouseId {get;set;}
    public string WarehouseCode {get;set;} = null!;
    public string WarehouseName {get;set;} = null!;
    public int PeriodId {get;set;}
    public string PeriodCode {get;set;} = null!;
    public string PeriodName {get;set;} = null!;
    public Guid CreatedBy {get;set;}
    public string CreatedUserName {get;set;} = null!;
    public DateTime CreatedAt {get;set;}
    public Guid? UpdatedBy {get;set;}
    public string? UpdatedUserName {get;set;} = null!;
    public DateTime? UpdatedAt {get;set;}
    public string? Note {get;set;}
    public List<GetInventoryOpeningLineResponse> Lines {get;set;} = [];
}
