using System.ComponentModel;
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

namespace Application.Features.GoodsReceipts.Create;

public class CreateGoodsReceiptHandler : IRequestHandler<CreateGoodsReceiptCommand, CreateDocumentResponse>
{
    private readonly DbSession _dbSession;
    private readonly CurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public CreateGoodsReceiptHandler(DbSession dbSession
        , CurrentUser currentUser
        , DocumentNoService documentNoService)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
        _docNoService = documentNoService;
    }

    const string INSERT_MASTER_SQL = @"
    INSERT INTO public.goods_receipts(
	      document_id
        , shipper_name
        , warehouse_id
    )
	VALUES (
          @DocumentId
        , @ShipperName
        , @WarehouseId
    );
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

    public async Task<CreateDocumentResponse> Handle(CreateGoodsReceiptCommand request, CancellationToken cancellationToken)
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
        var docNo = await _docNoService.GetNextDocumentNo("GR", request.DocumentDate.Year, request.DocumentDate.Month);

        await _dbSession.Connection.ExecuteAsync(DocumentSqls.INSERT_DOCUMENT_SQL, new CreateDocumentParams
        {
              DocumentId = id
            , DocumentNo = docNo
            , PostingDate = request.PostingDate
            , DocumentDate = request.DocumentDate
            , PeriodId = request.PeriodId
            , DocumentType = DocumentType.NK.ToString()
            , CreatedBy = _currentUser.UserId
            , Note = request.Note
            , Status = request.Status.ToString()
        }, _dbSession.Transaction);

        await _dbSession.Connection.ExecuteAsync(INSERT_MASTER_SQL, new
        {
              DocumentId = id
            , ShipperName = request.ShipperName
            , WarehouseId = request.WarehouseId
        }, _dbSession.Transaction);

        var lines = request.Lines.Select(p => new
        {
              DocumentId = id
            , ProductId = p.ProductId
            , UnitId = p.UnitId
            , DocumentQuantity = p.DocumentQuantity
            , ActualQuantity = p.ActualQuantity
            , Amount = p.Amount
            , SortOrder = p.SortOrder
            , Note = p.Note
            , UnitPrice = p.UnitPrice
        });

        await _dbSession.Connection.ExecuteAsync(INSERT_LINES_SQL, lines, _dbSession.Transaction);

        var balances = request.Lines.Select(p => new InventoryBalanceParams
        {
              WarehouseId = request.WarehouseId
            , ProductId = p.ProductId
            , UnitId = p.UnitId
            , Quantity = p.ActualQuantity
            , Amount = p.Amount
        });

        await _dbSession.Connection.ExecuteAsync(InventoryBalanceSqls.UPSERT_INVENTORY_BALANCE_SQL
            , balances, _dbSession.Transaction);

        return new CreateDocumentResponse
        {
            DocumentId = id,
            DocumentNo = docNo
        };
    }
}
