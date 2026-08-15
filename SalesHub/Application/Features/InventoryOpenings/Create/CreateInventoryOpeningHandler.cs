using Application.Database;
using Application.Services;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.InventoryOpenings.Create;

public class CreateInventoryOpeningHandler : IRequestHandler<CreateInventoryOpeningCommand, CreateInventoryOpeningResponse>
{
    private readonly DbSession _dbSession;
    private readonly CurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public CreateInventoryOpeningHandler(DbSession dbSession
        , CurrentUser currentUser
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
    , sort_order)
	VALUES (@DocumentId
    , @ProductId
    , @UnitId
    , @Quantity
    , @Amount
    , @SortOrder);
    ";

    const string INSERT_INVENTORY_BALANCE_SQL = @"
    INSERT INTO inventory_balances
    (warehouse_id, product_id, unit_id, quantity, amount)
    VALUES (@WarehouseId, @ProductId, @UnitId, @Quantity, @Amount)
    ON CONFLICT (warehouse_id, product_id, unit_id)
    DO UPDATE
    SET quantity = @Quantity, amount = @Amount;
    ";

    public async Task<CreateInventoryOpeningResponse> Handle(CreateInventoryOpeningCommand request, CancellationToken cancellationToken)
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

        var balances = request.Lines.Select(p => new
        {
            WarehouseId = request.WarehouseId,
            ProductId = p.ProductId,
            UnitId = p.UnitId,
            Quantity = p.Quantity,
            Amount = p.Amount,
        });

        await _dbSession.Connection.ExecuteAsync(INSERT_INVENTORY_BALANCE_SQL, balances, _dbSession.Transaction);

        return new CreateInventoryOpeningResponse
        {
            DocumentId = id,
            DocumentNo = docNo
        };
    }
}
