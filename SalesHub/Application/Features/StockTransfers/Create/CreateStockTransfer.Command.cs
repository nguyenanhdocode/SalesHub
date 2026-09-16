using System.Data;
using Application.Interfaces.Database;
using Application.Models.Documents;
using MediatR;

namespace Application.Features.StockTransfers.Create;

public class CreateStockTransferCommand : CreateDocumentCommand, IRequest<CreateDocumentResponse>
    , ITransactionalRequest
{
    public int SourceWarehouseId {get;set;}
    public int TargetWarehouseId {get;set;}
    public List<CreateStockTransferLineInput> Lines {get;set;} = [];
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
