using System.Data;
using Application.Database;
using Application.Interfaces.Database;
using Application.Models.Documents;
using MediatR;

namespace Application.Features.StockTransfers.Create;

public class CreateStockTransferHandler : IRequestHandler<CreateStockTransferCommand, CreateDocumentResponse>
{
    private readonly DbSession _dbSession;
    public CreateStockTransferHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public Task<CreateDocumentResponse> Handle(CreateStockTransferCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
