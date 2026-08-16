using System.ComponentModel;
using System.Data;
using Application.Database;
using Application.Exceptions;
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

    const string INSERT_LINES_SQL = @"
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
    ";

    const string GET_LINES_SQL = @"
    SELECT 
          product_id AS ProductId
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

    const string DELETE_LINES_SQL = @"
    DELETE FROM goods_receipt_lines
    WHERE document_id = @DocumentId AND product_id = @ProductId  AND unit_id = @UnitId;
    ";

    const string UPDATE_LINES_SQL = @"
    UPDATE public.goods_receipt_lines
	SET product_id = @ProductId
    , unit_id = @UnitId
    , document_quantity = @DocumentQuantity
    , actual_quantity = @ActualQuantity
    , amount = @Amount
    , sort_order = @SortOrder
    , note = @Note
    , unit_price = @UnitPrice
	WHERE document_id = @DocumentId AND product_id = @ProductId  AND unit_id = @UnitId;
    ";

    const string GET_WAREHOUSE_ID_SQL = @"
    SELECT warehouse_id
    FROM goods_receipts
    WHERE document_id = @DocumentId;
    ";

    const string UPDATE_BALANCE_LINES_SQL = @"
    UPDATE inventory_balances
    SET quantity = quantity + @QuantityDelta, amount = amount + @AmountDelta
    WHERE warehouse_id = @WarehouseId AND product_id = @ProductId AND unit_id = @UnitId;
    ";

    public async Task Handle(UpdateGoodsReceiptCommand request, CancellationToken cancellationToken)
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

        await _dbSession.Connection.ExecuteAsync(DocumentSqls.UPDATE_DOCUMENT_SQL, new
        {
            DocumentId = request.DocumentId,
            PostingDate = request.PostingDate,
            DocumentDate = request.DocumentDate,
            PeriodId = request.PeriodId,
            UpdatedBy = _currentUser.UserId,
            Note = request.Note,
            Status = request.Status.ToString()
        }, _dbSession.Transaction);

        await _dbSession.Connection.ExecuteAsync(UPDATE_MASTER_SQL, new
        {
            DocumentId = request.DocumentId,
            ShipperName = request.ShipperName
        }, _dbSession.Transaction);

        var dbLines = await _dbSession.Connection.QueryAsync<GoodsReceiptLineDto>(GET_LINES_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        var deletedRows = dbLines.ExceptBy(request.Lines.Select(p => (p.ProductId, p.UnitId)), p => (p.ProductId, p.UnitId))
            .Select(p => new
            {
                DocumentId = request.DocumentId,
                ProductId = p.ProductId,
                UnitId = p.UnitId,
                DocumentQuantity = p.DocumentQuantity,
                ActualQuantity = p.ActualQuantity,
                Amount = p.Amount
            }).ToList();

        var insertRows = request.Lines.ExceptBy(dbLines.Select(p => (p.ProductId, p.UnitId)), p => (p.ProductId, p.UnitId))
            .Select(p => new
            {
                DocumentId = request.DocumentId,
                ProductId = p.ProductId,
                UnitId = p.UnitId,
                DocumentQuantity = p.DocumentQuantity,
                ActualQuantity = p.ActualQuantity,
                Amount = p.Amount,
                SortOrder = p.SortOrder,
                Note = p.Note,
                UnitPrice = p.UnitPrice
            }).ToList();

        var updateRows = dbLines.Join(request.Lines
            , db => (db.ProductId, db.UnitId)
            , req => (req.ProductId, req.UnitId)
            , (db, req) => new
            {
                DocumentId = request.DocumentId,
                ProductId = req.ProductId,
                UnitId = req.UnitId,
                DocumentQuantity = req.DocumentQuantity,
                ActualQuantity = req.ActualQuantity,
                Amount = req.Amount,
                SortOrder = req.SortOrder,
                Note = req.Note,
                UnitPrice = req.UnitPrice,
                DocumentQuantityDelta = req.DocumentQuantity - db.DocumentQuantity,
                ActualQuantityDelta = req.ActualQuantity - db.ActualQuantity,
                AmountDelta = req.Amount - db.Amount
            })
            .ToList();

        var updateBalanceLines = new List<object>();

        int warehouseId = await _dbSession.Connection.ExecuteScalarAsync<int>(GET_WAREHOUSE_ID_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        if (deletedRows.Count > 0)
        {
            updateBalanceLines.AddRange(deletedRows.Select(p => new
            {
                WarehouseId = warehouseId,
                ProductId = p.ProductId,
                UnitId = p.UnitId,
                QuantityDelta = -p.ActualQuantity,
                AmountDelta = -p.Amount
            }));

            await _dbSession.Connection.ExecuteAsync(DELETE_LINES_SQL, deletedRows, _dbSession.Transaction);
        }

        if (updateRows.Count > 0)
        {
            updateBalanceLines.AddRange(updateRows.Select(p => new
            {
                WarehouseId = warehouseId,
                ProductId = p.ProductId,
                UnitId = p.UnitId,
                QuantityDelta = p.ActualQuantityDelta,
                AmountDelta = p.AmountDelta
            }));

            await _dbSession.Connection.ExecuteAsync(UPDATE_LINES_SQL, updateRows, _dbSession.Transaction);
        }

        if (insertRows.Count > 0)
        {
            updateBalanceLines.AddRange(insertRows.Select(p => new
            {
                WarehouseId = warehouseId,
                ProductId = p.ProductId,
                UnitId = p.UnitId,
                QuantityDelta = p.ActualQuantity,
                AmountDelta = p.Amount
            }));

            await _dbSession.Connection.ExecuteAsync(INSERT_LINES_SQL, insertRows, _dbSession.Transaction);
        }

        if (updateBalanceLines.Count > 0)
        {
            await _dbSession.Connection.ExecuteAsync(UPDATE_BALANCE_LINES_SQL, updateBalanceLines, _dbSession.Transaction);
        }
    }
}
