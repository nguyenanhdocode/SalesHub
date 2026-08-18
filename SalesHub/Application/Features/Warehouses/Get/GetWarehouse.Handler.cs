using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.Warehouses.Get;

public class GetWarehouseHandler : IRequestHandler<GetWarehouseQuery, GetWarehouseResponse>
{
    private readonly DbSession _dbSession;
    public GetWarehouseHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string GET_SQL = @"
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
    WHERE warehouse_id = @WarehouseId;
    ";
    public async Task<GetWarehouseResponse> Handle(GetWarehouseQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await _dbSession.Connection.QuerySingleOrDefaultAsync<GetWarehouseResponse>(GET_SQL, new
        {
            WarehouseId = request.WarehouseId
        });

        if (warehouse == null)
        {
            throw new BusinessException("notfound");
        }

        return warehouse;
    }
}
