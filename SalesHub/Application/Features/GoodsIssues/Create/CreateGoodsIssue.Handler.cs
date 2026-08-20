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

namespace Application.Features.GoodsIssues.Create;

public class CreateGoodsIssueHandler : IRequestHandler<CreateGoodsIssueCommand, CreateDocumentResponse>
{
    private readonly DbSession _dbSession;
    private readonly ICurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public CreateGoodsIssueHandler(DbSession dbSession
        , ICurrentUser currentUser
        , DocumentNoService docNoService)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
        _docNoService = docNoService;
    }

    const string INSERT_MASTER_SQL = @"
    INSERT INTO public.goods_issues(
	    document_id, warehouse_id, reason
    )
	VALUES (@DocumentId, @WarehouseId, @Reason);
    ";

    const string INSERT_LINES_SQL = @"
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
    );
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
        SET quantity = quantity - x.Quantity
            , amount = amount - x.Amount
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

    public async Task<CreateDocumentResponse> Handle(CreateGoodsIssueCommand request, CancellationToken cancellationToken)
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

        var id = Guid.CreateVersion7();
        var docNo = await _docNoService.GetNextDocumentNo("GI"
            , request.DocumentDate.Year
            , request.DocumentDate.Month);

        await _dbSession.Connection.ExecuteAsync(DocumentSqls.INSERT_DOCUMENT_SQL, new CreateDocumentParams
        {
            DocumentId = id,
            DocumentNo = docNo,
            PostingDate = request.PostingDate,
            DocumentDate = request.DocumentDate,
            PeriodId = request.PeriodId,
            DocumentType = DocumentType.XK.ToString(),
            CreatedBy = _currentUser.UserId,
            Note = request.Note,
            Status = request.Status.ToString()
        }, _dbSession.Transaction);

        await _dbSession.Connection.ExecuteAsync(INSERT_MASTER_SQL, new
        {
            DocumentId = id,
            WarehouseId = request.WarehouseId,
            Reason = request.Reason
        }, _dbSession.Transaction);

        var lines = request.Lines.Select(p => new
        {
            DocumentId = id,
            ProductId = p.ProductId,
            UnitId = p.UnitId,
            DocumentQuantity = p.DocumentQuantity,
            ActualQuantity = p.ActualQuantity,
            Amount = p.Amount,
            SortOrder = p.SortOrder,
            Note = p.Note,
            UnitPrice = p.UnitPrice
        });

        await _dbSession.Connection.ExecuteAsync(INSERT_LINES_SQL, lines, _dbSession.Transaction);

        if (request.Status == DocumentStatus.POSTED)
        {
            var updateBalances = request.Lines.Select(p => new
            {
                WarehouseId = request.WarehouseId,
                ProductId = p.ProductId,
                UnitId = p.UnitId,
                ActualQuantity = p.ActualQuantity,
                Amount = p.Amount
            });

            var failedRows = await _dbSession.Connection.QueryAsync<int>(UPDATE_BALANCE_SQL
                , updateBalances
                , _dbSession.Transaction);

            if (failedRows.Any())
            {
                throw new BusinessException("insufficient_inventory");
            }
        }

        return new CreateDocumentResponse
        {
            DocumentId = id,
            DocumentNo = docNo
        };
    }
}
