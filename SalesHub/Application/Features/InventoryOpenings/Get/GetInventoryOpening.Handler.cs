using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.InventoryOpenings.Get;

public class GetInventoryOpeningHandler : IRequestHandler<GetInventoryOpeningQuery, GetInventoryOpeningResponse>
{
    private readonly DbSession _dbSession;
    public GetInventoryOpeningHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string GET_MASTER_SQL = @"
    SELECT
        inventory_openings.document_id AS DocumentId
        , inventory_openings.document_no AS DocumentNo
        , warehouses.branch_id AS BranchId
        , branchs.code AS BranchCode
        , branchs.name AS BranchName
        , inventory_openings.warehouse_id AS WarehouseId
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
    WHERE inventory_openings.document_id = @DocumentId;
    ";

    const string GET_LINES_SQL = @"
    SELECT
        inventory_opening_lines.product_id AS ProductId
        , products.internal_code AS ProductInternalCode
        , products.name AS ProductName
        , units.unit_id AS UnitId
        , units.code AS UnitCode
        , units.name AS UnitName
        , inventory_opening_lines.quantity AS Quantity
        , inventory_opening_lines.amount AS Amount
        , inventory_opening_lines.sort_order AS SortOrder
    FROM inventory_opening_lines
    INNER JOIN products ON products.product_id = inventory_opening_lines.product_id
    INNER JOIN units ON units.unit_id = inventory_opening_lines.unit_id
    WHERE inventory_opening_lines.document_id = @DocumentId
    ";

    public async Task<GetInventoryOpeningResponse> Handle(GetInventoryOpeningQuery request, CancellationToken cancellationToken)
    {
        var document = await _dbSession.Connection.QuerySingleOrDefaultAsync<GetInventoryOpeningResponse>(GET_MASTER_SQL, new
        {
            DocumentId = request.DocumentId
        });

        if (document == null)
        {
            throw new BusinessException("notfound");
        }

        var lines = await _dbSession.Connection.QueryAsync<GetInventoryOpeningLineResponse>(GET_LINES_SQL, new
        {
            DocumentId = request.DocumentId
        });

        document.Lines = lines.ToList();

        return document;
    }
}
