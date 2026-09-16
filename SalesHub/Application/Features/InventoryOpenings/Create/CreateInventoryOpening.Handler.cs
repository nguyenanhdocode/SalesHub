using System.Text.Json;
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
        , note
        , automated_generate
    )
	VALUES (
          @DocumentId
        , @DocumentNo
        , @WarehouseId
        , @PeriodId
        , @CreatedBy
        , @Note
        , false);
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

    public async Task<CreateDocumentResponse> Handle(CreateInventoryOpeningCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();

        string docNo = request.DocumentNo ?? "";

        if (string.IsNullOrEmpty(docNo))
        {
            docNo = await _docNoService.GetNextDocumentNo("IO"
            , DateTime.Now.Year
            , DateTime.Now.Month);
        }

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

        if (request.WriteToBalances)
        {
            var balances = request.Lines.Select(p => new
            {
                warehouse_id = request.WarehouseId,
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

        return new CreateDocumentResponse
        {
            DocumentId = id,
            DocumentNo = docNo
        };
    }
}
