namespace Application.Features.Products.List;

public class ProductDto
{
    public int ProductId {get;set;}
    public string InternalCode {get;set;} = null!;
    public string ExternalCode {get;set;} = null!;
    public string Name {get;set;} = null!;
    public string CostingMethod {get;set;} = null!;
    public int BaseUnitId {get;set;}
    public string BaseUnitName {get;set;} = null!;
    public bool Active {get;set;}
    public int SupplierId {get;set;}
    public string SupplierName {get;set;} = null!;
}
