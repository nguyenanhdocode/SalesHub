using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Products.Delete;

public class DeleteProductHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly DbSession _dbSession;
    public DeleteProductHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string DELETE_QUERY = @"
    DELETE FROM product_unit WHERE product_id = @ProductId;
    DELETE FROM unit_conversions WHERE product_id = @ProductId;
    DELETE FROM public.products WHERE product_id = @ProductId;
    ";

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_QUERY, new
        {
            ProductId = request.ProductId
        });
    }
}
