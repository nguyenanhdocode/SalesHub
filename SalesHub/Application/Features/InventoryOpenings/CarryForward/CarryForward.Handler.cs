using Application.Database;
using Application.Exceptions;
using Application.Services;
using Dapper;
using Infrastructure.Security;
using MediatR;

namespace Application.Features.InventoryOpenings.CarryForward;

public class CarryForwardHandler : IRequestHandler<CarryForwardCommand>
{
    private readonly DbSession _dbSession;
    private readonly CurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public CarryForwardHandler(DbSession dbSession
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

    const string CHECK_PERIOD_SQL = @"
    SELECT EXISTS(SELECT 1 FROM periods WHERE period_id = @PeriodId AND is_closed = true);
    ";

    const string INSERT_LINES_SQL = @"
    INSERT INTO inventory_opening_lines
    (document_id, product_id, unit_id, quantity, amount)
    SELECT
        @DocumentId
        , product_id
        , unit_id
        , quantity
        , amount
    FROM inventory_balances
    WHERE warehouse_id = @WarehouseId;
    ";

    public async Task Handle(CarryForwardCommand request, CancellationToken cancellationToken)
    {
        bool isSrcPeriodClosed = await _dbSession.Connection.QuerySingleOrDefaultAsync<bool>(CHECK_PERIOD_SQL, new
        {
            PeriodId = request.SrcPeriodId
        }, _dbSession.Transaction);

        if (!isSrcPeriodClosed)
        {
            throw new BusinessException("src_period_opening");
        }

        bool isDstPeriodClosed = await _dbSession.Connection.QuerySingleOrDefaultAsync<bool>(CHECK_PERIOD_SQL, new
        {
            PeriodId = request.DstPeriodId
        }, _dbSession.Transaction);

        if (isDstPeriodClosed)
        {
            throw new BusinessException("dst_period_closed");
        }

        foreach (int warehouseId in request.WarehouseIds)
        {
            // Insert master
            var id = Guid.CreateVersion7();

            var docNo = await _docNoService.GetNextDocumentNo("IO"
            , DateTime.Now.Year
            , DateTime.Now.Month);

            await _dbSession.Connection.ExecuteAsync(INSERT_MASTER_SQL, new
            {
                DocumentId = id,
                DocumentNo = docNo,
                WarehouseId = warehouseId,
                PeriodId = request.DstPeriodId,
                CreatedBy = _currentUser.UserId,
                Note = "PHIẾU TỒN ĐẦU KỲ ĐƯỢC SINH TỰ ĐỘNG"
            }, _dbSession.Transaction);

            // Insert lines
            await _dbSession.Connection.ExecuteAsync(INSERT_LINES_SQL, new
            {
                DocumentId = id,
                WarehouseId = warehouseId
            }, _dbSession.Transaction);
        }
    }
}
