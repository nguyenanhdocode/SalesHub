namespace Application.Features.Suppliers.List;

public class SupplierListItem
{
    public int SupplierId {get;set;}
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public string? ContactPerson {get;set;}
    public string? Phone {get;set;}
    public string? TaxCode {get;set;}
    public string? Email {get;set;}
    public string? Address {get;set;}
    public DateTime CreatedAt {get;set;}
    public DateTime? UpdatedAt {get;set;}
}
