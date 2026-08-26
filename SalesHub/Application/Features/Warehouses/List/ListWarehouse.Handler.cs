using System.Text;
using Application.Database;
using Application.Models.Common;
using Application.Shared;
using Dapper;
using MediatR;

namespace Application.Features.Warehouses.List;

public class ListWarehouseHandler : IRequestHandler<ListWarehouseQuery, PagedResult<WarehouseListItem>>
{
    private readonly DbSession _dbSession;
    public ListWarehouseHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string LIST_SQL = @"
    SELECT 
      warehouses.warehouse_id AS WarehouseId
    , warehouses.code AS Code
    , warehouses.name AS Name
    , warehouses.active AS Active
    , warehouses.created_at AS CreatedAt
    , warehouses.updated_at AS UpdatedAt
    , branchs.branch_id AS BranchId
    , branchs.code AS BranchCode
    , branchs.name AS BranchName
	FROM public.warehouses
    INNER JOIN branchs ON branchs.branch_id = warehouses.branch_id
    WHERE 1=1
    ";

    const string COUNTER_SQL = @"
    SELECT COUNT(1) FROM public.warehouses WHERE 1=1
    ";

    public async Task<PagedResult<WarehouseListItem>> Handle(ListWarehouseQuery request, CancellationToken cancellationToken)
    {
        var filterQueryBuilder = new StringBuilder();
        var parameters = new DynamicParameters();

        if (request.WarehouseId != null)
        {
            filterQueryBuilder.AppendLine(" AND warehouse_id = @WarehouseId");
            parameters.Add("WarehouseId", request.WarehouseId);
        }

        if (!string.IsNullOrEmpty(request.Code))
        {
            filterQueryBuilder.AppendLine(" AND warehouses.code ILIKE @Code");
            parameters.Add("Code", $"%{request.Code}%");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            filterQueryBuilder.AppendLine(" AND warehouses.name ILIKE @Name");
            parameters.Add("Name", $"%{request.Name}%");
        }

        if (request.Active != null)
        {
            filterQueryBuilder.AppendLine(" AND warehouses.active = @Active");
            parameters.Add("Active", request.Active);
        }

        var counterQueryBuilder = new StringBuilder(COUNTER_SQL);
        counterQueryBuilder.AppendLine(filterQueryBuilder.ToString());

        int totalRows = await _dbSession.Connection.ExecuteScalarAsync<int>(counterQueryBuilder.ToString(), parameters);
        int pageSize = request.PageSize > 0 ? request.PageSize : Constants.PAGE_SIZE;
        int totalPages = Convert.ToInt32(Math.Ceiling(totalRows / (double)pageSize));
        int pageNum = (request.PageNum > 0 && request.PageNum <= totalPages) ? request.PageNum : 1;

        var listQueryBuilder = new StringBuilder(LIST_SQL);
        listQueryBuilder.AppendLine(filterQueryBuilder.ToString());
        listQueryBuilder.AppendLine("ORDER BY warehouses.code OFFSET @Offset LIMIT @PageSize");
        parameters.Add("Offset", (pageNum - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var data = await _dbSession.Connection.QueryAsync<WarehouseListItem>(listQueryBuilder.ToString(), parameters);

        return new PagedResult<WarehouseListItem>(data, totalPages, pageNum, pageSize);
    }
}
