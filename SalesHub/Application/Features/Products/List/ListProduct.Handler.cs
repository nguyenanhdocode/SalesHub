using System.Text;
using Application.Database;
using Application.Models.Common;
using Dapper;
using MediatR;

namespace Application.Features.Products.List;

public class ListProductHandler : IRequestHandler<ListProductQuery, PagedResult<ProductListItem>>
{
    private readonly DbSession _dbSession;
    public ListProductHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string BASE_QUERY = @"
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
    WHERE 1=1
    ";

    private const string COUNTER_QUERY = @"
    SELECT COUNT(1) FROM products WHERE 1=1
    ";

    public async Task<PagedResult<ProductListItem>> Handle(ListProductQuery request, CancellationToken cancellationToken)
    {
        var filterQueryBuilder = new StringBuilder();
        var parameters = new DynamicParameters();

        if (request.ProductId != null)
        {
            filterQueryBuilder.AppendLine(" AND products.product_id = @ProductId");
            parameters.Add("ProductId", request.ProductId);
        }

        if (!string.IsNullOrEmpty(request.InternalCode))
        {
            filterQueryBuilder.AppendLine(" AND products.internal_code ILIKE @InternalCode");
            parameters.Add("InternalCode", $"%{request.InternalCode}%");
        }

        if (!string.IsNullOrEmpty(request.ExternalCode))
        {
            filterQueryBuilder.AppendLine(" AND products.external_code ILIKE @ExternalCode");
            parameters.Add("ExternalCode", $"{request.ExternalCode}");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            filterQueryBuilder.AppendLine(" AND products.name ILIKE @Name");
            parameters.Add("Name", $"{request.Name}");
        }

        if (request.BaseUnitIds != null)
        {
            filterQueryBuilder.AppendLine(" AND products.base_unit_id =Any(@BaseUnitIds)");
            parameters.Add("BaseUnitIds", request.BaseUnitIds);
        }

        if (request.Active != null)
        {
            filterQueryBuilder.AppendLine(" AND products.active = @Active");
            parameters.Add("Active", request.Active);
        }

        if (request.SupplierId != null)
        {
            filterQueryBuilder.AppendLine(" AND products.supplier_id = @SupplierId");
            parameters.Add("SupplierId", request.SupplierId);
        }

        var counterQueryBuilder = new StringBuilder(COUNTER_QUERY);
        counterQueryBuilder.AppendLine(filterQueryBuilder.ToString());

        int totalRows = await _dbSession.Connection.ExecuteScalarAsync<int>(counterQueryBuilder.ToString(), parameters);
        int totalPages = Convert.ToInt32(Math.Ceiling(totalRows / (double)request.PageSize));

        var dataQueryBuilder = new StringBuilder(BASE_QUERY);
        dataQueryBuilder.AppendLine(filterQueryBuilder.ToString());
        dataQueryBuilder.AppendLine("ORDER BY internal_code OFFSET @Offset LIMIT @PageSize");
        parameters.Add("Offset", (request.PageNum - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);

        var data = await _dbSession.Connection.QueryAsync<ProductListItem>(dataQueryBuilder.ToString(), parameters);

        return new PagedResult<ProductListItem>(data, totalPages, request.PageNum, request.PageSize);
    }
}
