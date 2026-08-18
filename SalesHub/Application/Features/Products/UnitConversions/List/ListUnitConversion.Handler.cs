using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Products.UnitConversions.List;

public class ListUnitConversionsHandler : IRequestHandler<ListUnitConversionsQuery, IEnumerable<UnitConversionListItem>>
{
    private readonly DbSession _dbSession;
    public ListUnitConversionsHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string LIST_SQL = @"
    SELECT
        units_src.unit_id AS SrcUnitId
        , units_src.name AS SrcUnitName
        , units_dst.unit_id AS DstUnitId
        , units_dst.name AS DstUnitName
        , unit_conversions.conversion_factor AS ConversionFactor
    FROM unit_conversions
    INNER JOIN units AS units_src ON units_src.unit_id = unit_conversions.src_unit_id
    INNER JOIN units AS units_dst ON units_dst.unit_id = unit_conversions.dst_unit_id
    WHERE unit_conversions.product_id = @ProductId;
    ";

    public async Task<IEnumerable<UnitConversionListItem>> Handle(ListUnitConversionsQuery request, CancellationToken cancellationToken)
    {
        var units = await _dbSession.Connection.QueryAsync<UnitConversionListItem>(LIST_SQL, new
        {
            ProductId = request.ProductId
        });

        return units;
    }
}
