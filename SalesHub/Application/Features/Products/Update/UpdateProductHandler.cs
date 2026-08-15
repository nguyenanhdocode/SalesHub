using System.Text;
using Application.Database;
using Dapper;
using FluentValidation;
using MediatR;

namespace Application.Features.Products.Update;

public class UpdatePrdocutHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly DbSession _dbSession;
    public UpdatePrdocutHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string UPDATE_SQL = @"
    UPDATE public.products
	SET 
      internal_code=@InternalCode
    , external_code=@ExternalCode
    , name=@Name
    , costing_method=@CostingMethod
    , base_unit_id=@BaseUnitId
    , updated_at=CURRENT_TIMESTAMP
    , active=@Active
    , supplier_id=@SupplierId
	WHERE product_id=@ProductId;
    ";

    const string GET_UNITS_BY_PRODUCT_ID_SQL = @"
    SELECT unit_id FROM product_unit WHERE product_id = @ProductId
    ";

    const string DELETE_UNITS_SQL = @"
    DELETE FROM product_unit WHERE product_id = @ProductId AND unit_id = ANY(@UnitIds);
    ";

    const string INSERT_SQL = @"
    INSERT INTO product_unit (product_id, unit_id)
    VALUES (@ProductId, @UnitId);
    ";

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(UPDATE_SQL, request, _dbSession.Transaction);

        var dbUnitIds = await _dbSession.Connection.QueryAsync<int>(GET_UNITS_BY_PRODUCT_ID_SQL, new
        {
            ProductId = request.ProductId
        });

        var deleteUnitIds = dbUnitIds.Except(request.UnitIds).ToList();

        if (deleteUnitIds.Any())
        {
            await _dbSession.Connection.ExecuteAsync(DELETE_UNITS_SQL, new
            {
                ProductId = request.ProductId,
                UnitIds = deleteUnitIds
            }, _dbSession.Transaction);
        }

        var insertUnits = request.UnitIds.Except(dbUnitIds)
            .Select(p => new
            {
                ProductId = request.ProductId,
                UnitId = p
            })
            .ToList();

        if (insertUnits.Any())
        {

            await _dbSession.Connection.ExecuteAsync(INSERT_SQL, insertUnits, _dbSession.Transaction);
        }
    }
}
