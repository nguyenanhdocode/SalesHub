using Application.Database;
using Application.Interfaces.Security;
using Application.Models.Documents;
using Application.Models.InventoryBalances;
using Application.Services;
using Application.Shared;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.InventoryOpenings.Create;

public class CreateInventoryOpeningHandler : IRequestHandler<CreateInventoryOpeningCommand, CreateDocumentResponse>
{
    private readonly DbSession _dbSession;
    private readonly ICurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public CreateInventoryOpeningHandler(DbSession dbSession
        , ICurrentUser currentUser
        , DocumentNoService docNoService)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
        _docNoService = docNoService;
    }

    const string INSERT_MASTER_SQL = @"
    INSERT INTO public.inventory_openings(
          document_id
        , document_no
        , warehouse_id
        , period_id
        , created_by
        , note)
	VALUES (
          @DocumentId
        , @DocumentNo
        , @WarehouseId
        , @PeriodId
        , @CreatedBy
        , @Note);
    ";

    const string INSERT_LINE_SQL = @"
    INSERT INTO public.inventory_opening_lines(
          document_id
        , product_id
        , unit_id
        , quantity
        , amount
        , sort_order
    )
	VALUES (
          @DocumentId
        , @ProductId
        , @UnitId
        , @Quantity
        , @Amount
        , @SortOrder
    );
    ";

    public async Task<CreateDocumentResponse> Handle(CreateInventoryOpeningCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();

        var docNo = await _docNoService.GetNextDocumentNo("IO"
            , DateTime.Now.Year
            , DateTime.Now.Month);

        await _dbSession.Connection.ExecuteAsync(INSERT_MASTER_SQL, new
        {
            DocumentId = id,
            DocumentNo = docNo,
            WarehouseId = request.WarehouseId,
            PeriodId = request.PeriodId,
            CreatedBy = _currentUser.UserId,
            Note = request.Note

        }, _dbSession.Transaction);

        var lines = request.Lines.Select(p => new
        {
            DocumentId = id,
            ProductId = p.ProductId,
            UnitId = p.UnitId,
            Quantity = p.Quantity,
            Amount = p.Amount,
            SortOrder = p.SortOrder
        });

        await _dbSession.Connection.ExecuteAsync(INSERT_LINE_SQL, lines, _dbSession.Transaction);

        var balances = request.Lines.Select(p => new InventoryBalanceParams
        {
            WarehouseId = request.WarehouseId,
            ProductId = p.ProductId,
            UnitId = p.UnitId,
            Quantity = p.Quantity,
            Amount = p.Amount,
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
