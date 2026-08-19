using Application.Database;
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
        UPDATE inventory_balances AS target
        SET
            quantity = target.quantity - source.actual_quantity
            , amount = target.amount - source.amount
        FROM (
            SELECT
                gr.warehouse_id
                , line.product_id
                , line.unit_id
                , SUM(line.actual_quantity) AS actual_quantity
                , SUM(line.amount) AS amount
            FROM goods_receipt_lines AS line
            INNER JOIN goods_receipts AS gr ON gr.document_id = line.document_id
            WHERE line.document_id = @DocumentId
            GROUP BY gr.warehouse_id, line.product_id, line.unit_id
        ) AS source
        WHERE target.warehouse_id = source.warehouse_id
        AND target.product_id = source.product_id
        AND target.unit_id = source.unit_id
        AND target.quantity >= source.actual_quantity
        AND target.amount >= source.amount
        RETURNING target.warehouse_id, target.product_id, target.unit_id;
    ";

    const string GET_LINE_COUNT = @"
    SELECT COUNT(1) 
    FROM goods_receipt_lines
    WHERE document_id = @DocumentId
    ";

    public async Task Handle(DeleteGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        int lineCount = await _dbSession.Connection.ExecuteScalarAsync<int>(GET_LINE_COUNT, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        int affectedRows = await _dbSession.Connection.ExecuteAsync(UPDATE_BALANCES_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        if (affectedRows != lineCount)
        {
            throw new Exception("update_balances_failed");
        }
    }
}
