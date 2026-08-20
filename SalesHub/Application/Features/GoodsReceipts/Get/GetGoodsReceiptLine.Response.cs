namespace Application.Features.GoodsReceipts.Get;

public class GetGoodsReceiptLineResponse
{
    public int ProductId {get;set;}
    public string ProductInternalCode {get;set;} = null!;
    public string ProductName {get;set;} = null!;
    public int UnitId {get;set;}
    public string UnitCode {get;set;} = null!;
    public string UnitName {get;set;} = null!;
    public int DocumentQuantity {get;set;}
    public int ActualQuantity {get;set;}
    public decimal Amount {get;set;}
    public string? Note {get;set;}
    public decimal UnitPrice {get;set;}
}
