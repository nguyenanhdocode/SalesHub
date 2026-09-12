using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.GoodsReceipts.Get;

public class GetGoodsReceiptHandler : IRequestHandler<GetGoodsReceiptQuery, GetGoodsReceiptResponse>
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
    INNER JOIN goods_receipts ON goods_receipts.document_id = documents.document_id
    INNER JOIN periods ON periods.period_id = documents.period_id
    INNER JOIN users AS users_created ON users_created.user_id = documents.created_by
    INNER JOIN warehouses ON warehouses.warehouse_id = goods_receipts.warehouse_id
    INNER JOIN branchs ON branchs.branch_id = warehouses.branch_id
	LEFT JOIN users AS users_deleted ON users_deleted.user_id = documents.deleted_by
	LEFT JOIN users AS users_updated ON users_updated.user_id = documents.updated_by
    WHERE documents.document_id = @DocumentId
    ";

    const string GET_LINES_SQL = @"
    SELECT
        goods_receipt_lines.product_id AS ProductId
        , products.internal_code AS ProductInternalCode
        , products.name AS ProductName
        , units.unit_id AS UnitId
        , units.code AS UnitCode
        , units.name AS UnitName
		, goods_receipt_lines.document_quantity AS DocumentQuantity
        , goods_receipt_lines.actual_quantity AS ActualQuantity
        , goods_receipt_lines.amount AS Amount
        , goods_receipt_lines.sort_order AS SortOrder
		, goods_receipt_lines.note AS Note
        , goods_receipt_lines.unit_price AS UnitPrice
    FROM goods_receipt_lines
    INNER JOIN products ON products.product_id = goods_receipt_lines.product_id
    INNER JOIN units ON units.unit_id = goods_receipt_lines.unit_id
    WHERE goods_receipt_lines.document_id = @DocumentId;
    ";

    public async Task<GetGoodsReceiptResponse> Handle(GetGoodsReceiptQuery request, CancellationToken cancellationToken)
    {
        var row = await _dbSession.Connection.QuerySingleOrDefaultAsync<GetGoodsReceiptResponse>(GET_SQL, request);

        if (row == null)
        {
            throw new BusinessException("notfound");
        }

        var lines = await _dbSession.Connection.QueryAsync<GetGoodsReceiptLineResponse>(GET_LINES_SQL, new
        {
            DocumentId = request.DocumentId
        });

        row.Lines = lines.ToList();

        return row;
    }
}
