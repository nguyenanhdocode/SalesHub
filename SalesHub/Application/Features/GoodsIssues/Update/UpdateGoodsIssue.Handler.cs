using System.Text;
using System.Text.Json;
using Application.Database;
using Application.Exceptions;
using Application.Models.Documents;
using Application.Models.InventoryBalances;
using Application.Services;
using Application.Shared;
using Application.Shared.Documents;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.GoodsIssues.Update;

public class UpdateGoodsIssueHandler : IRequestHandler<UpdateGoodsIssueCommand>
{
    private readonly DbSession _dbSession;
    private readonly CurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public UpdateGoodsIssueHandler(DbSession dbSession
        , CurrentUser currentUser
        , DocumentNoService docNoService)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
        _docNoService = docNoService;
    }

    const string GET_OLD_STATUS = @"
    SELECT status
    FROM documents WHERE document_id = @DocumentId;
    ";

    const string UPDATE_MASTER_SQL = @"
    UPDATE goods_issues SET reason = @Reason
    WHERE document_id = @DocumentId;
    ";

    const string GET_LINES_SQL = @"
    SELECT 
          document_id AS DocumentId
        , product_id AS ProductId
        , unit_id AS UnitId
        , document_quantity AS DocumentQuantity
        , actual_quantity AS ActualQuantity
        , amount AS Amount
        , sort_order AS SortOrder
        , note AS Note
        , unit_price AS UnitPrice
    FROM goods_issue_lines
    WHERE document_id = @DocumentId;
    ";

    const string UPSERT_LINES_SQL = @"
    INSERT INTO public.goods_issue_lines(
	      document_id
        , product_id
        , unit_id
        , document_quantity
        , actual_quantity
        , amount
        , sort_order
        , note
        , unit_price
    )
	VALUES (
          @DocumentId
        , @ProductId
        , @UnitId
        , @DocumentQuantity
        , @ActualQuantity
        , @Amount
        , @SortOrder
        , @Note
        , @UnitPrice
    )
    ON CONFLICT (document_id, product_id, unit_id)
    DO UPDATE SET
        document_quantity = EXCLUDED.document_quantity
        , actual_quantity = EXCLUDED.actual_quantity
        , amount = EXCLUDED.amount
        , sort_order = EXCLUDED.sort_order
        , note = EXCLUDED.note
        , unit_price = EXCLUDED.unit_price
    ";

    const string DELETE_LINE_SQL = @"
    DELETE FROM goods_issue_lines
    WHERE document_id = @DocumentId AND product_id = @ProductId AND unit_id = @UnitId;
    ";

    const string UPDATE_BALANCE_SQL = @"
    WITH lines AS (
        SELECT *
        FROM jsonb_to_record(@Lines::jsonb) AS x (
            WarehouseId int,
            ProductId int,
            UnitId int,
            Quantity int,
            Amount numeric
        )
    )
    , updated AS (
        UPDATE inventory_balances AS target
        SET quantity = quantity + x.Quantity
            , amount = amount + x.Amount
        WHERE target.warehouse_id = lines.WarehouseId 
            AND target.product_id = lines.product_id
            AND target.unit_id = lines.unit_id
        RETURNING target.warehouse_id, target.product_id, target.quantity
    )
    SELECT DISTINCT lines.product_id
    FROM lines
    LEFT JOIN updated ON updated.warehouse_id = lines.warehouse_id
    AND updated.product_id = lines.product_id
    AND updated.unit_id = lines.unit_id
    WHERE updated.product_id IS NULL OR COALESCE(updated.quantity, -1) < 0
    ";

    const string GET_WAREHOUSE_ID_SQL = @"
    SELECT warehouse_id
    FROM goods_issues
    WHERE document_id = @DocumentId;
    ";

    public async Task Handle(UpdateGoodsIssueCommand request, CancellationToken cancellationToken)
    {
        bool isValidPostingDate = await _dbSession.Connection.ExecuteScalarAsync<bool>(DocumentSqls.CHECK_POSTINGDATE_SQL, new
        {
            PeriodId = request.PeriodId,
            PostingDate = request.PostingDate
        }, _dbSession.Transaction);

        if (!isValidPostingDate)
        {
            throw new BusinessException("invalid_postingdate");
        }

        // Lấy trạng thái hiện tại của phiếu
        var oldStatus = await _dbSession.Connection.ExecuteScalarAsync<string>(GET_OLD_STATUS, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        // Nếu phiếu đã ở trạng thái posted thì không cho cập nhật trạng thái
        string newStatus = oldStatus == DocumentStatus.POSTED.ToString() ? oldStatus : request.Status.ToString();

        await _dbSession.Connection.ExecuteAsync(DocumentSqls.UPDATE_DOCUMENT_SQL, new UpdateDocumentParams
        {
            DocumentId = request.DocumentId,
            PostingDate = request.PostingDate,
            DocumentDate = request.DocumentDate,
            PeriodId = request.PeriodId,
            UpdatedBy = _currentUser.UserId,
            Note = request.Note,
            Status = newStatus
        }, _dbSession.Transaction);

        await _dbSession.Connection.ExecuteAsync(UPDATE_MASTER_SQL, new
        {
            Reason = request.Reason,
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        var dbLines = await _dbSession.Connection.QueryAsync<GoodsIssueRow>(GET_LINES_SQL
            , new
            {
                DocumentId = request.DocumentId
            }, _dbSession.Transaction);

        var deleteRows = dbLines.ExceptBy(request.Lines.Select(p => (p.ProductId, p.UnitId)), p => (p.ProductId, p.UnitId))
            .ToList();

        if (deleteRows.Any())
        {
            await _dbSession.Connection.ExecuteAsync(DELETE_LINE_SQL, deleteRows, _dbSession.Transaction);
        }

        var upsertRows = request.Lines;

        if (upsertRows.Any())
        {
            await _dbSession.Connection.ExecuteAsync(UPSERT_LINES_SQL, upsertRows, _dbSession.Transaction);
        }

        int warehouseId = await _dbSession.Connection.ExecuteScalarAsync<int>(GET_WAREHOUSE_ID_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        var updateBalances = deleteRows.Select(p => new InventoryBalanceParams
        {
            WarehouseId = warehouseId,
            ProductId = p.ProductId,
            UnitId = p.UnitId,
            Quantity = p.ActualQuantity,
            Amount = p.Amount
        })
        .Union(upsertRows.LeftJoin(dbLines
            , p => (p.ProductId, p.UnitId)
            , p => (p.ProductId, p.UnitId)
            , (req, db) => new InventoryBalanceParams
            {
                WarehouseId = warehouseId,
                ProductId = req.ProductId,
                UnitId = req.UnitId,
                Quantity = -(req.ActualQuantity - (db != null ? db.ActualQuantity : 0)),
                Amount = -(req.Amount - (db != null ? db.Amount : 0)),
            }))
        .ToList();

        var failedRows = await _dbSession.Connection.QueryAsync<int>(UPDATE_BALANCE_SQL
        , new
        {
            Lines = JsonSerializer.Serialize(updateBalances)
        }
        , _dbSession.Transaction);

        if (failedRows.Any())
        {
            throw new BusinessException("insufficient_inventory");
        }
    }
}
