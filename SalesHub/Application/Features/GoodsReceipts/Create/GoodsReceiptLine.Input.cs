namespace Application.Features.GoodsReceipts.Create;

public class GoodsReceiptLineInput
{
    public int ProductId {get;set;}
    public int UnitId {get;set;}
    public int DocumentQuantity {get;set;}
    public int ActualQuantity {get;set;}
    public decimal Amount {get;set;}
    public int? SortOrder {get;set;}
    public string? Note {get;set;}
    public decimal UnitPrice {get;set;}
}
