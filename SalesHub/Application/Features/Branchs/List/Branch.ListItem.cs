namespace Application.Features.Branchs.List;

public class BranchListItem
{
    public string BranchId {get;set;} = null!;
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public string? Address {get;set;}
    public string? Phone {get;set;}
    public string? Email {get;set;}
    public string? TaxCode {get;set;}
    public DateTime CreatedAt {get;set;}
    public DateTime? UpdatedAt {get;set;}
}
