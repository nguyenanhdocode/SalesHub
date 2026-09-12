using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Products.Create;

public class CreatePrdocutHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly DbSession _dbSession;
    public CreatePrdocutHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string INSERT_QUERY = @"
    INSERT INTO public.products(
          internal_code
        , external_code
        , name
        , costing_method
        , base_unit_id
        , supplier_id
        , vat_rate
    )
	VALUES (
        @InternalCode
        , @ExternalCode
        , @Name
        , @CostingMethod
        , @BaseUnitId
        , @SupplierId
        , @VatRate
    )
    RETURNING product_id;
    ";

    const string INSERT_PRODUCT_UNIT_SQL = @"
    INSERT INTO product_unit (product_id, unit_id)
    VALUES (@ProductId, @UnitId)
    ON CONFLICT DO NOTHING;
    ";

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(INSERT_QUERY, request, _dbSession.Transaction);

        var productUnits = new List<int>
        {
            request.BaseUnitId
        };
        productUnits.AddRange(request.UnitIds);

        var inserts = productUnits.Distinct().Select(p => new
        {
            ProductId = id,
            UnitId = p
        }).ToList();

        await _dbSession.Connection.ExecuteAsync(INSERT_PRODUCT_UNIT_SQL, inserts, _dbSession.Transaction);

        return id;
    }
}
