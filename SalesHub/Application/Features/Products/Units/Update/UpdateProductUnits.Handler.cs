using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.Products.Units.Update;

public class UpdateProductUnitsHandler : IRequestHandler<UpdateProductUnitsCommand>
{
    private readonly DbSession _dbSession;
    public UpdateProductUnitsHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string GET_UNITS_SQL = @"
    SELECT unit_id FROM product_unit WHERE product_id = @ProductId;
    ";

    const string DELETE_UNITS_SQL = @"
    DELETE FROM product_unit WHERE product_id = @ProductId
    AND unit_id = ANY(@UnitIds);
    ";

    const string INSERT_UNITS_SQL = @"
    INSERT INTO product_unit (product_id, unit_id)
    VALUES (@ProductId, @UnitId)
    ON CONFLICT DO NOTHING;
    ";

    const string GET_BASE_UNIT_SQL = @"
    SELECT base_unit_id FROM products WHERE product_id = @ProductId
    ";

    public async Task Handle(UpdateProductUnitsCommand request, CancellationToken cancellationToken)
    {
        var dbUnits = await _dbSession.Connection.QueryAsync<int>(GET_UNITS_SQL
        , new
        {
            ProductId = request.ProductId
        }, _dbSession.Transaction);

        var deleteUnits = dbUnits.Except(request.UnitIds).ToList();
        int baseUnitId = await _dbSession.Connection.ExecuteScalarAsync<int>(GET_BASE_UNIT_SQL
        , new
        {
            ProductId = request.ProductId
        }, _dbSession.Transaction);

        if (deleteUnits.Contains(baseUnitId))
        {
            throw new BusinessException("base_unit_delete_restrict");
        }

        if (deleteUnits.Any())
        {
            await _dbSession.Connection.ExecuteAsync(DELETE_UNITS_SQL
           , new
           {
               UnitIds = deleteUnits,
               ProductId = request.ProductId
           }, _dbSession.Transaction);
        }

        var insertUnits = request.UnitIds.Except(dbUnits)
            .Select(p => new
            {
                ProductId = request.ProductId,
                UnitId = p
            })
            .ToList();

        await _dbSession.Connection.ExecuteAsync(INSERT_UNITS_SQL
        , insertUnits, _dbSession.Transaction);
    }
}
