namespace Application.Features.Branchs.Get;

public class BranchDto
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
