using Application.Database;
using Application.Exceptions;
using Application.Models.InventoryBalances;
using Application.Shared;
using Dapper;
using MediatR;

namespace Application.Features.GoodsReceipts.Delete;

public class DeleteGoodsReceiptHandler : IRequestHandler<DeleteGoodsReceiptCommand>
{
    private readonly DbSession _dbSession;
    public DeleteGoodsReceiptHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string DELETE_SQL = @"
    DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId;
    DELETE FROM goods_receipts WHERE document_id = @DocumentId;
    DELETE FROM documents WHERE document_id = @DocumentId;
    ";

    const string UPDATE_BALANCES_SQL = @"
    WITH lines AS (
        SELECT
            goods_receipts.warehouse_id
            , goods_receipt_lines.product_id
            , goods_receipt_lines.unit_id
            , goods_receipt_lines.actual_quantity
            , goods_receipt_lines.amount
        FROM goods_receipt_lines
        INNER JOIN goods_receipts ON goods_receipts.document_id = goods_receipt_lines.document_id
        WHERE goods_receipt_lines.document_id = @DocumentId 
    )
    , updated AS (
        UPDATE inventory_balances AS ib
        SET
        FROM lines 
        WHERE ib.warehouse_id = lines.warehouse_id AND ib.product_id = lines.product_id
        AND ib.unit_id = lines.unit_id AND ib.quantity >= lines.actual_quantity
        AND ib.amount >= lines.amount
        RETURNING ib.warehouse_id, ib.product_id, ib.unit_id
    )
    SELECT
        lines.product_id
        , lines.unit_id
    FROM lines
    LEFT JOIN updated ON updated.warehouse_id = lines.warehouse_id
        AND updated.product_id = lines.product_id
        AND updated.uint_id = lines.unit_id
    WHERE updated.product_id IS NULL
    ";

    const string GET_STATUS_SQL = @"
    SELECT status FROM documents WHERE document_id = @DocumentId;
    ";

    public async Task Handle(DeleteGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        string? status = await _dbSession.Connection.ExecuteScalarAsync<string>(GET_STATUS_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        if (status == DocumentStatus.POSTED.ToString())
        {
            var failedRows = await _dbSession.Connection.QueryAsync<object>(UPDATE_BALANCES_SQL, new
            {
                DocumentId = request.DocumentId
            }, _dbSession.Transaction);

            if (failedRows.Any())
            {
                throw new BusinessException("insufficient_inventory");
            }
        }
    }
}
