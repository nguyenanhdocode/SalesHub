using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.GoodsReceipts.Get;

public class GetGoodsReceiptHandler : IRequestHandler<GetGoodsReceiptQuery, GoodsReceiptsDto>
{
    private readonly DbSession _dbSession;
    public GetGoodsReceiptHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string GET_SQL = @"
    SELECT
          documents.document_id AS DocumentId
        , documents.document_no AS DocumentNo
        , documents.posting_date AS PostingDate
        , documents.document_date AS DocumentDate
        , documents.period_id AS PeriodId
        , periods.code AS PeriodCode
        , periods.name AS PeriodName
        , documents.created_at AS CreatedAt
        , users_created.username AS CreatedUsername
        , documents.deleted_at AS DeletedAt
        , documents.status AS Status
        , goods_receipts.shipper_name AS ShipperName
        , goods_receipts.warehouse_id AS WarehouseId
        , warehouses.code AS WarehouseCode
        , warehouses.name AS WarehouseName
        , warehouses.branch_id AS BranchId
        , branchs.code AS BranchCode
        , branchs.name AS BranchName
		, users_deleted.username AS DeletedUsername
		, users_updated.username AS UpdatedUsername
		, documents.updated_at AS UpdatedAt
    FROM documents
    INNER JOIN goods_receipts ON documents.document_id = documents.document_id
    INNER JOIN periods ON periods.period_id = documents.period_id
    INNER JOIN users AS users_created ON users_created.user_id = documents.created_by
    INNER JOIN warehouses ON warehouses.warehouse_id = goods_receipts.warehouse_id
    INNER JOIN branchs ON branchs.branch_id = warehouses.branch_id
	LEFT JOIN users AS users_deleted ON users_deleted.user_id = documents.deleted_by
	LEFT JOIN users AS users_updated ON users_updated.user_id = documents.updated_by
    WHERE documents.document_id = @DocumentId
    ";

    public async Task<GoodsReceiptsDto> Handle(GetGoodsReceiptQuery request, CancellationToken cancellationToken)
    {
        var row = await _dbSession.Connection.QuerySingleOrDefaultAsync<GoodsReceiptsDto>(GET_SQL, request);

        if (row == null)
        {
            throw new BusinessException("notfound");
        }

        return row;
    }
}
