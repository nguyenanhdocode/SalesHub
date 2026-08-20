using System.ComponentModel;
using System.Data;
using System.Text.Json;
using Application.Database;
using Application.Exceptions;
using Application.Features.GoodsReceipts.Models;
using Application.Models.Documents;
using Application.Models.InventoryBalances;
using Application.Services;
using Application.Shared;
using Application.Shared.Documents;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.GoodsReceipts.Update;

public class UpdateGoodsReceiptHandler : IRequestHandler<UpdateGoodsReceiptCommand>
{
    private readonly DbSession _dbSession;
    private readonly CurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public UpdateGoodsReceiptHandler(DbSession dbSession
        , CurrentUser currentUser
        , DocumentNoService documentNoService)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
        _docNoService = documentNoService;
    }

    private const string UPDATE_MASTER_SQL = @"
    UPDATE goods_receipts SET shipper_name  = @ShipperName
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
	FROM public.goods_receipt_lines
    WHERE document_id = @DocumentId;
    ";

    const string DELETE_LINE_SQL = @"
    DELETE FROM goods_receipt_lines
    WHERE document_id = @DocumentId AND product_id = @ProductId  AND unit_id = @UnitId;
    ";

    const string GET_WAREHOUSE_ID_SQL = @"
    SELECT warehouse_id
    FROM goods_receipts
    WHERE document_id = @DocumentId;
    ";

    const string UPSERT_LINE_SQL = @"
    INSERT INTO public.goods_receipt_lines(
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
          document_quantity = @DocumentQuantity
        , actual_quantity = @ActualQuantity
        , amount = @Amount
        , note = @Note
        , unit_price = @UnitPrice
    ";

    public const string UPSERT_INVENTORY_BALANCE_SQL = @"
    WITH upserted AS (
        INSERT INTO inventory_balances
        (
              warehouse_id
            , product_id
            , unit_id
            , quantity
            , amount
        )
        SELECT
        FROM jsonb_to_recordset(@Lines::jsonb) AS x (
            WarehouseId int,
            ProductId int,
            UnitId int,
            Quantity int,
            Amount numeric
        )
        ON CONFLICT (warehouse_id, product_id, unit_id)
        DO UPDATE SET
              quantity = ib.quantity + EXCLUDED.quantity
            , amount   = ib.amount + EXCLUDED.amount
        RETURNING warehouse_id, product_id, unit_id, quantity
    )
    SELECT
        DISTINCT product_id
    FROM upserted
    WHERE quantity < 0;
    ";

    const string GET_OLD_STATUS = @"
    SELECT status
    FROM documents WHERE document_id = @DocumentId;
    ";

    public async Task Handle(UpdateGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        // Kiểm tra posting date có nằm trong khoảng của kỳ kế toán hay không
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

        // Update bảng goods_receipts
        await _dbSession.Connection.ExecuteAsync(UPDATE_MASTER_SQL, new
        {
            DocumentId = request.DocumentId,
            ShipperName = request.ShipperName
        }, _dbSession.Transaction);

        var dbLines = await _dbSession.Connection.QueryAsync<GoodsReceiptLineRow>(GET_LINES_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        var deletedRows = dbLines.ExceptBy(request.Lines.Select(p => (p.ProductId, p.UnitId)), p => (p.ProductId, p.UnitId))
            .ToList();

        if (deletedRows.Any())
        {
            await _dbSession.Connection.ExecuteAsync(DELETE_LINE_SQL, deletedRows, _dbSession.Transaction);
        }

        var upsertRows = request.Lines.Select(p => new
        {
            DocumentId = request.DocumentId,
            ProductId = p.ProductId,
            UnitId = p.UnitId,
            ActualQuantity = p.ActualQuantity,
            DocumentQuantity = p.DocumentQuantity,
            Amount = p.Amount,
            SortOrder = p.SortOrder,
            Note = p.Note,
            UnitPrice = p.UnitPrice
        });

        if (upsertRows.Any())
        {
            await _dbSession.Connection.ExecuteAsync(UPSERT_LINE_SQL, upsertRows, _dbSession.Transaction);
        }

        // Nếu trạng thái của phiếu là POSTED thì mới cập nhật số dư
        if (newStatus == DocumentStatus.POSTED.ToString())
        {
            int warehouseId = await _dbSession.Connection.ExecuteScalarAsync<int>(GET_WAREHOUSE_ID_SQL, new
            {
                DocumentId = request.DocumentId
            }, _dbSession.Transaction);

            var upsertBalances = deletedRows
            .Select(p => new
            {
                WarehouseId = warehouseId,
                ProductId = p.ProductId,
                UnitId = p.UnitId,
                Quantity = -p.ActualQuantity,
                Amount = -p.Amount
            })
            .Union(upsertRows.LeftJoin(dbLines
                , p => (p.ProductId, p.UnitId)
                , p => (p.ProductId, p.UnitId)
                , (req, db) => new
                {
                    WarehouseId = warehouseId,
                    ProductId = req.ProductId,
                    UnitId = req.UnitId,
                    Quantity = req.ActualQuantity - (db != null ? db.ActualQuantity : 0),
                    Amount = req.Amount - (db != null ? db.Amount : 0),
                }))
            .ToList();

            var failedRows = await _dbSession.Connection.QueryAsync<int>(UPSERT_INVENTORY_BALANCE_SQL
                , new
                {
                    Lines = JsonSerializer.Serialize(upsertBalances)
                }
                , _dbSession.Transaction);

            if (failedRows.Any())
            {
                throw new BusinessException("insufficient_inventory");
            }
        }
    }
}
