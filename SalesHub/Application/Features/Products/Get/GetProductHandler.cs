using Application.Database;
using Application.Exceptions;
using Application.Models.Common;
using Dapper;
using MediatR;

namespace Application.Features.Products.Get;

public class GetProductHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    private readonly DbSession _dbSession;
    public GetProductHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string GET_QUERY = @"
    SELECT 
        products.product_id AS ProductId
        , products.internal_code AS InternalCode
        , products.external_code AS ExternalCode
        , products.name AS Name
        , products.costing_method AS CostingMethod
        , products.base_unit_id AS BaseUnitId
        , units.name AS BaseUnitName
        , products.active AS Active
        , products.created_at AS CreatedAt
        , products.updated_at AS UpdatedAt
        , products.supplier_id AS SupplierId
        , suppliers.name AS SupplierName
    FROM public.products
    LEFT JOIN units ON units.unit_id = products.base_unit_id
    LEFT JOIN suppliers ON suppliers.supplier_id = products.supplier_id
    WHERE product_id = @ProductId;
    ";

    private const string GET_UNITS_QUERY = @"
    SELECT units.unit_id AS UnitId
    , units.code AS Code
    , units.name AS Name
    FROM product_unit
    INNER JOIN units ON units.unit_id = product_unit.unit_id
    WHERE product_unit.product_id = @ProductId;
    ";

    public async Task<ProductDto> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var product = await _dbSession.Connection.QuerySingleOrDefaultAsync<ProductDto>(GET_QUERY, new
        {
            ProductId = request.ProductId
        });

        if (product == null)
        {
            throw new BusinessException("notfound");
        }

        var units = await _dbSession.Connection.QueryAsync<UnitDto>(GET_UNITS_QUERY, new
        {
            ProductId = request.ProductId
        });

        product.Units = units.OrderBy(p => p.Code);

        return product;
    }
}
