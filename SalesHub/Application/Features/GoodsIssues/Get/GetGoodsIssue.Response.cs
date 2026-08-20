namespace Application.Features.GoodsIssues.Get;

public class GetGoodsIssueResponse
{
    public Guid DocumentId {get;set;}
    public string DocumentNo {get;set;} = null!;
    public DateTime PostingDate {get;set;}
    public DateTime DocumentDate {get;set;}
    public int PeriodId {get;set;}
    public string PeriodCode {get;set;} = null!;
    public string PeriodName {get;set;} = null!;
    public DateTime CreatedAt {get;set;}
    public string CreatedUsername {get;set;} = null!;
    public DateTime? DeletedAt {get;set;}
    public string? DeletedUsername {get;set;}
    public DateTime? UpdatedAt {get;set;}
    public string? UpdatedUsername {get;set;}
    public string Status {get;set;} = null!;
    public string? Reason {get;set;}
    public int WarehouseId {get;set;}
    public string WarehouseCode {get;set;} = null!;
    public string WarehouseName {get;set;} = null!;
    public List<GetGoodsIssueLineResponse> Lines {get;set;} = [];
}
