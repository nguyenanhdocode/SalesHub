namespace Application.Features.StockTransfers.Create;

public class UnitConversionRow
{
    public int ProductId {get;set;}
    public decimal ConversionFactor {get;set;}
    public int DstUnitId {get;set;}
    public int SrcUnitId {get;set;}
}
