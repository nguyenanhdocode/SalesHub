namespace Application.Features.Products.UnitConversions.List;

public class UnitConversionListItem
{
    public int SrcUnitId {get;set;}
    public string SrcUnitName {get;set;} = null!;
    public int DstUnitId {get;set;}
    public string DstUnitName {get;set;} = null!;
    public decimal ConversionFactor {get;set;}
}
