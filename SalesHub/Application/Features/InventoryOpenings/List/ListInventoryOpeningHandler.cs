using System.Data;
using System.Text;
using Application.Database;
using Application.Interfaces.Database;
using Application.Models.Common;
using Dapper;
using MediatR;

namespace Application.Features.InventoryOpenings.List;

public class ListInventoryOpeningHandler : IRequestHandler<ListInventoryOpeningQuery, PagedResult<InventoryOpeningDto>>
{
    private readonly DbSession _dbSession;
    public ListInventoryOpeningHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string BASE_SQL = @"
    SELECT
        inventory_openings.document_id AS DocumentId
        , inventory_openings.document_no AS DocumentNo
        , warehouses.branch_id AS BranchId
        , branchs.code AS BranchCode
        , branchs.name AS BranchName
        , warehouses.warehouse_id AS WarehouseId
        , warehouses.code AS WarehouseCode
        , warehouses.name AS WarehouseName
        , inventory_openings.period_id AS PeriodId
        , periods.code AS PeriodCode
        , periods.name AS PeriodName
        , inventory_openings.created_by AS CreatedBy
        , users_created.username AS CreatedUserName
        , inventory_openings.created_at AS CreatedAt
        , inventory_openings.updated_by AS UpdatedBy
        , users_updated.username AS UpdatedUserName
        , inventory_openings.updated_at AS UpdatedAt
        , inventory_openings.note AS Note
    FROM inventory_openings
    INNER JOIN warehouses ON warehouses.warehouse_id = inventory_openings.warehouse_id
    INNER JOIN branchs ON branchs.branch_id = warehouses.branch_id
    INNER JOIN periods ON periods.period_id = inventory_openings.period_id
    INNER JOIN users AS users_created ON users_created.user_id = inventory_openings.created_by
    LEFT JOIN users AS users_updated ON users_updated.user_id = inventory_openings.updated_by
    WHERE 1=1
    ";

    const string COUNTER_SQL = @"
    SELECT COUNT(1) FROM inventory_openings WHERE 1=1
    ";

    public async Task<PagedResult<InventoryOpeningDto>> Handle(ListInventoryOpeningQuery request, CancellationToken cancellationToken)
    {
        var filterBuilder = new StringBuilder();

        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(request.DocumentNo))
        {
            filterBuilder.AppendLine("AND inventory_openings.document_no ILIKE @DocumentNo");
            parameters.Add("DocumentNo", $"%{request.DocumentNo}%");
        }

        if (request.BranchIds.Count > 0)
        {
            filterBuilder.AppendLine("AND warehouses.branch_id = ANY(@BranchIds)");
            parameters.Add("BranchIds", request.BranchIds);
        }

        if (request.WarehouseIds.Count > 0)
        {
            filterBuilder.AppendLine("AND inventory_openings.warehouse_id = ANY(@WarehouseIds)");
            parameters.Add("WarehouseIds", request.WarehouseIds);
        }

        if (request.PeriodIds.Count > 0)
        {
            filterBuilder.AppendLine("AND inventory_openings.period_id = ANY(@PeriodIds)");
            parameters.Add("PeriodIds", request.PeriodIds);
        }

        if (!string.IsNullOrEmpty(request.CreatedBy))
        {
            filterBuilder.AppendLine("AND users_created.username ILIKE @CreatedBy");
            parameters.Add("CreatedBy", $"%{request.CreatedBy}%");
        }

        var counterQuery = new StringBuilder(COUNTER_SQL);
        counterQuery.AppendLine(filterBuilder.ToString());

        int totalRows = await _dbSession.Connection.ExecuteScalarAsync<int>(counterQuery.ToString(), parameters);
        int totalPages = Convert.ToInt32(Math.Ceiling(totalRows / (double)request.PageSize));

        var dataQuery = new StringBuilder(BASE_SQL);
        dataQuery.AppendLine(filterBuilder.ToString());
        dataQuery.AppendLine("ORDER BY CreatedAt OFFSET @Offset LIMIT @PageSize");
        parameters.Add("Offset", (request.PageNum - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);

        var data = await _dbSession.Connection.QueryAsync<InventoryOpeningDto>(dataQuery.ToString(), parameters);

        return new PagedResult<InventoryOpeningDto>(data, totalPages, request.PageNum, request.PageSize);
    }
}
