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
    , base_unit_id=@BaseUnitId
    , updated_at=CURRENT_TIMESTAMP
    , active=@Active
    , supplier_id=@SupplierId
    , vat_rate = @VatRate
	WHERE product_id=@ProductId;
    ";

    const string UPSERT_UNITS_SQL = @"
    INSERT INTO product_unit (product_id, unit_id)
    VALUES (@ProductId, @UnitId)
    ON CONFLICT DO NOTHING;
    ";

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(UPDATE_SQL, request, _dbSession.Transaction);

        await _dbSession.Connection.ExecuteAsync(UPSERT_UNITS_SQL
        , new
        {
            ProductId = request.ProductId,
            UnitId = request.BaseUnitId
        }, _dbSession.Transaction);
    }
}
