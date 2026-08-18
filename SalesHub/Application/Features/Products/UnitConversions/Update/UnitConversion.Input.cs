namespace Application.Features.Products.UnitConversions.Update;
public class UnitConversionInput
{
    public int SrcUnitId {get;set;}
    public int DstUnitId {get;set;}
    public decimal ConversionFactor {get;set;}
}
