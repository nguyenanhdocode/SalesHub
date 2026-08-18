using Application.Database;
using Application.Exceptions;
using Application.Services;
using Application.Shared;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.InventoryOpenings.Update;

public class UpdateInventoryOpeningHandler : IRequestHandler<UpdateInventoryOpeningCommand>
{
    private readonly DbSession _dbSession;
    private readonly CurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public UpdateInventoryOpeningHandler(DbSession dbSession
        , CurrentUser currentUser
        , DocumentNoService docNoService)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
        _docNoService = docNoService;
    }

    const string UPDATE_MASTER_SQL = @"
    UPDATE inventory_openings
    SET note = @Note
    WHERE document_id = @DocumentId;
    ";

    const string GET_LINES_SQL = @"
    SELECT
        product_id AS ProductId
        , unit_id AS UnitId
        , quantity AS Quantity
        , amount AS Amount
        , sort_order AS SortOrder
    FROM inventory_opening_lines
    WHERE document_id = @DocumentId;
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

    const string UPDATE_LINE_SQL = @"
    UPDATE inventory_opening_lines
    SET product_id = @ProductId
    , unit_id = @UnitId
    , quantity = @Quantity
    , amount = @Amount
    , sort_order = @SortOrder
    WHERE document_id = @DocumentId AND product_id = @ProductId AND unit_id = @UnitId
    ";

    const string DELETE_LINE_SQL = @"
    DELETE FROM inventory_opening_lines
    WHERE document_id = @DocumentId AND product_id = @ProductId AND unit_id = @UnitId
    ";

    const string CHECK_HAS_DOCS_SQL = @"
    SELECT EXISTS(SELECT 1
    FROM inventory_openings 
    INNER JOIN documents ON documents.period_id = inventory_openings.period_id
    WHERE inventory_openings.document_id = @DocumentId);
    ";

    const string GET_WAREHOUSE_ID_SQL = @"
    SELECT warehouse_id FROM inventory_openings WHERE document_id = @DocumentId;
    ";

    public async Task Handle(UpdateInventoryOpeningCommand request, CancellationToken cancellationToken)
    {
        bool hasDocs = await _dbSession.Connection.ExecuteScalarAsync<bool>(CHECK_HAS_DOCS_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        if (hasDocs)
        {
            throw new BusinessException("period_has_transactions");
        }

        await _dbSession.Connection.ExecuteAsync(UPDATE_MASTER_SQL, new
        {
            DocumentId = request.DocumentId,
            Note = request.Note
        }, _dbSession.Transaction);

        var dbLines = await _dbSession.Connection.QueryAsync<InventoryOpeningLineRow>(GET_LINES_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        var deleteLines = dbLines.ExceptBy(request.Lines.Select(p => (p.ProductId, p.UnitId))
            , p => (p.ProductId, p.UnitId)).ToList();

        var insertLines = request.Lines.ExceptBy(dbLines.Select(p => (p.ProductId, p.UnitId))
            , p => (p.ProductId, p.UnitId));

        var updateLines = request.Lines.IntersectBy(dbLines.Select(p => (p.ProductId, p.UnitId))
            , p => (p.ProductId, p.UnitId)).ToList();

        if (deleteLines.Any())
        {
            await _dbSession.Connection.ExecuteAsync(DELETE_LINE_SQL, deleteLines, _dbSession.Transaction);
        }

        if (updateLines.Any())
        {
            await _dbSession.Connection.ExecuteAsync(UPDATE_LINE_SQL, updateLines, _dbSession.Transaction);
        }

        if (insertLines.Any())
        {
            await _dbSession.Connection.ExecuteAsync(INSERT_LINE_SQL, insertLines, _dbSession.Transaction);
        }

        int warehouseId = await _dbSession.Connection.QuerySingleAsync<int>(GET_WAREHOUSE_ID_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        var balances = request.Lines.Select(p => new
        {
            WarehouseId = warehouseId,
            ProductId = p.ProductId,
            UnitId = p.UnitId,
            Quantity = p.Quantity,
            Amount = p.Amount,
        });

        await _dbSession.Connection.ExecuteAsync(InventoryBalanceSqls.UPSERT_INVENTORY_BALANCE_SQL
            , balances, _dbSession.Transaction);
    }
}
