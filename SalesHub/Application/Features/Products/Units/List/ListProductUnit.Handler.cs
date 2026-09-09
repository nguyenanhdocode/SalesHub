using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Products.Units.List;

public class ListProductUnitHandler : IRequestHandler<ListProductUnitQuery, IEnumerable<ProductUnitListItem>>
{
    private readonly DbSession _dbSession;
    public ListProductUnitHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string LIST_SQL = @"
    SELECT
        product_unit.unit_id AS UnitId
        , units.code
        , units.name
        , CASE WHEN products.base_unit_id = product_unit.unit_id THEN 1 ELSE 0 END IsBaseUnit
    FROM product_unit
    INNER JOIN units ON units.unit_id = product_unit.unit_id
    INNER JOIN products ON products.product_id = product_unit.product_id
    WHERE product_unit.product_id = @ProductId;
    ";

    public async Task<IEnumerable<ProductUnitListItem>> Handle(ListProductUnitQuery request, CancellationToken cancellationToken)
    {
        var units = await _dbSession.Connection.QueryAsync<ProductUnitListItem>(LIST_SQL
        , new
        {
            ProductId = request.ProductId
        });

        return units;
    }
}
