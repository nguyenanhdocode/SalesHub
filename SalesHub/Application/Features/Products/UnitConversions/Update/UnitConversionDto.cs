namespace Application.Features.Products.UnitConversions.Update;
public class UnitConversionDto
{
    public int SrcUnitId {get;set;}
    public int DstUnitId {get;set;}
    public decimal ConversionFactor {get;set;}
}
