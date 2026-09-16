using System.Text.Json;
using Application.Database;
using Application.Exceptions;
using Application.Interfaces.Security;
using Application.Services;
using Application.Shared;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.InventoryOpenings.Update;

public class UpdateInventoryOpeningHandler : IRequestHandler<UpdateInventoryOpeningCommand>
{
    private readonly DbSession _dbSession;
    private readonly ICurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;
    private readonly QueryService _queryService;

    public UpdateInventoryOpeningHandler(DbSession dbSession
        , ICurrentUser currentUser
        , DocumentNoService docNoService
        , QueryService queryService)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
        _docNoService = docNoService;
        _queryService = queryService;
    }

    const string UPDATE_MASTER_SQL = @"
    UPDATE inventory_openings
    SET note = @Note
    , updated_by = @UpdatedBy
    , updated_at = CURRENT_TIMESTAMP
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

    const string UPSERT_LINE_SQL = @"
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
    )
    ON CONFLICT (document_id, product_id, unit_id)
    DO UPDATE SET 
          quantity = EXCLUDED.quantity
        , amount = EXCLUDED.amount;
    ";

    const string DELETE_LINE_SQL = @"
    DELETE FROM inventory_opening_lines
    WHERE document_id = @DocumentId AND product_id = @ProductId AND unit_id = @UnitId
    ";

    const string UPSERT_BALANCES_SQL = @"
    WITH lines AS (
        SELECT *
        FROM jsonb_to_recordset(@Lines::jsonb) AS x (
            warehouse_id int,
            product_id int,
            unit_id int,
            quantity int,
            amount numeric
        )
    )
    INSERT INTO inventory_balances AS ib
    (
          warehouse_id
        , product_id
        , unit_id
        , quantity
        , amount
    )
    SELECT * FROM lines
    ON CONFLICT (warehouse_id, product_id, unit_id)
    DO UPDATE SET quantity = EXCLUDED.quantity, amount = EXCLUDED.amount;
    ";

    public async Task Handle(UpdateInventoryOpeningCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(UPDATE_MASTER_SQL, new
        {
            DocumentId = request.DocumentId,
            Note = request.Note,
            UpdatedBy = _currentUser.UserId
        }, _dbSession.Transaction);

        var dbLines = await _dbSession.Connection.QueryAsync<InventoryOpeningLineRow>(GET_LINES_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        var deleteLines = dbLines.ExceptBy(request.Lines.Select(p => (p.ProductId, p.UnitId))
            , p => (p.ProductId, p.UnitId)).ToList();

        var upsertLines = request.Lines.Select(p => new
        {
            DocumentId = request.DocumentId,
            ProductId = p.ProductId,
            UnitId = p.UnitId,
            Quantity = p.Quantity,
            Amount = p.Amount,
            SortOrder = p.SortOrder
        });

        if (deleteLines.Any())
        {
            await _dbSession.Connection.ExecuteAsync(DELETE_LINE_SQL, deleteLines, _dbSession.Transaction);
        }

        if (upsertLines.Any())
        {
            await _dbSession.Connection.ExecuteAsync(UPSERT_LINE_SQL, upsertLines, _dbSession.Transaction);
        }

        int warehouseId = await _queryService.GetColumnValue<int>("inventory_openings", "document_id"
            , request.DocumentId.ToString(), "warehouse_id");

        if (request.WriteToBalances)
        {
            var balances = request.Lines.Select(p => new
            {
                warehouse_id = warehouseId,
                product_id = p.ProductId,
                unit_id = p.UnitId,
                quantity = p.Quantity,
                amount = p.Amount,
            });

            await _dbSession.Connection.ExecuteAsync(UPSERT_BALANCES_SQL
            , new
            {
                Lines = JsonSerializer.Serialize(balances)
            }
            , _dbSession.Transaction);
        }
    }
}
