using Application.Models.Documents;
using MediatR;

namespace Application.Features.StockTransfers.Create;

public class CreateStockTransferLineInput
{
    public int ProductId {get;set;}
    public int SrcUnitId {get;set;}
    public int DstUnitId {get;set;}
    public int SrcQuantity {get;set;}
    public string? Note {get;set;}
}
