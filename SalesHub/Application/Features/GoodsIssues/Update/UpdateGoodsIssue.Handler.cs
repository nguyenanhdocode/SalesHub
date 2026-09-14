using System.Text;
using System.Text.Json;
using Application.Database;
using Application.Exceptions;
using Application.Interfaces.Security;
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
    private readonly ICurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public UpdateGoodsIssueHandler(DbSession dbSession
        , ICurrentUser currentUser
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
        FROM jsonb_to_recordset(@Lines::jsonb) AS x (
            warehouse_id int,
            product_id int,
            unit_id int,
            quantity int,
            amount numeric
        )
    ),
    updated AS (
        UPDATE inventory_balances AS ib
        SET quantity = ib.quantity - lines.quantity
        , amount = ib.amount - lines.amount
        FROM lines
        WHERE lines.warehouse_id = ib.warehouse_id AND lines.product_id = ib.product_id
        AND lines.unit_id = ib.unit_id
        RETURNING ib.warehouse_id, ib.product_id, ib.unit_id, ib.quantity
    )
    SELECT COUNT(lines.product_id)
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

        var upsertRows = request.Lines.Select(p => new
        {
            DocumentId = request.DocumentId
            ,
            ProductId = p.ProductId
            ,
            UnitId = p.UnitId
            ,
            DocumentQuantity = p.DocumentQuantity
            ,
            ActualQuantity = p.ActualQuantity
            ,
            Amount = p.ActualQuantity * p.UnitPrice
            ,
            SortOrder = p.SortOrder
            ,
            Note = p.Note
            ,
            UnitPrice = p.UnitPrice
        }).ToList();

        if (upsertRows.Any())
        {
            await _dbSession.Connection.ExecuteAsync(UPSERT_LINES_SQL, upsertRows, _dbSession.Transaction);
        }

        if (newStatus == DocumentStatus.POSTED.ToString())
        {
            int warehouseId = await _dbSession.Connection.ExecuteScalarAsync<int>(GET_WAREHOUSE_ID_SQL, new
            {
                DocumentId = request.DocumentId
            }, _dbSession.Transaction);

            var balanceLines = new object();

            if (oldStatus == DocumentStatus.DRAFT.ToString())
            {
                balanceLines = request.Lines.Select(p => new
                {
                    warehouse_id = warehouseId,
                    product_id = p.ProductId,
                    unit_id = p.UnitId,
                    quantity = p.ActualQuantity,
                    amount = p.ActualQuantity * p.UnitPrice
                })
                .ToList();
            }
            else
            {
                balanceLines = deleteRows.Select(p => new
                {
                    warehouse_id = warehouseId,
                    product_id = p.ProductId,
                    unit_id = p.UnitId,
                    quantity = -p.ActualQuantity,
                    amount = -p.Amount
                })
                .Union(upsertRows.LeftJoin(dbLines
                    , p => (p.ProductId, p.UnitId)
                    , p => (p.ProductId, p.UnitId)
                    , (req, db) => new
                    {
                        warehouse_id = warehouseId,
                        product_id = req.ProductId,
                        unit_id = req.UnitId,
                        quantity = req.ActualQuantity - (db == null ? 0 : db.ActualQuantity),
                        amount = req.ActualQuantity * req.UnitPrice - (db == null ? 0 : db.ActualQuantity * db.UnitPrice)
                    }))
                .ToList();
            }

            int failedCount = await _dbSession.Connection.ExecuteScalarAsync<int>(UPDATE_BALANCE_SQL
                , new
                {
                    Lines = JsonSerializer.Serialize(balanceLines)
                }, _dbSession.Transaction);

            if (failedCount > 0)
            {
                throw new BusinessException("insufficient_inventory");
            }
        }
    }
}
