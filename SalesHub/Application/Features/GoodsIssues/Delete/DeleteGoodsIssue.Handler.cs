using Application.Database;
using Application.Exceptions;
using Application.Interfaces.Security;
using Application.Shared;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.GoodsIssues.Delete;

public class DeleteGoodsIssueHandle : IRequestHandler<DeleteGoodsIssueCommand>
{
    private readonly DbSession _dbSession;
    private readonly ICurrentUser _currentUser;

    public DeleteGoodsIssueHandle(DbSession dbSession
        , ICurrentUser currentUser)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
    }

    const string DELETE_SQL = @"
    UPDATE documents
    SET deleted_by = @DeletedBy, deleted_at = CURRENT_TIMESTAMP
    WHERE document_id = @DocumentId;
    ";

    const string UPDATE_BALANCES_SQL = @"
    WITH lines AS (
        SELECT
            goods_issues.warehouse_id
            , goods_issue_lines.product_id
            , goods_issue_lines.unit_id
            , goods_issue_lines.actual_quantity
            , goods_issue_lines.amount
        FROM goods_issue_lines
        INNER JOIN goods_issues ON goods_issues.document_id = goods_issue_lines.document_id
        WHERE goods_issue_lines.document_id = @DocumentId 
    )
    , updated AS (
        UPDATE inventory_balances AS ib
        SET quantity = quantity + lines.actual_quantity
        , amount = amount + lines.amount
        FROM lines 
        WHERE ib.warehouse_id = lines.warehouse_id AND ib.product_id = lines.product_id
        AND ib.unit_id = lines.unit_id AND ib.quantity >= lines.actual_quantity
        AND ib.amount >= lines.amount
        RETURNING ib.warehouse_id, ib.product_id, ib.unit_id
    )
    SELECT DISTINCT
        lines.product_id
    FROM lines
    LEFT JOIN updated ON updated.warehouse_id = lines.warehouse_id
        AND updated.product_id = lines.product_id
        AND updated.uint_id = lines.unit_id
    WHERE updated.product_id IS NULL
    ";

    const string GET_STATUS_SQL = @"
    SELECT status FROM documents WHERE document_id = @DocumentId;
    ";

    public async Task Handle(DeleteGoodsIssueCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_SQL, new
        {
            DeletedBy = _currentUser.UserId,
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
