using Application.Database;
using Application.Exceptions;
using Application.Features.Suppliers.Get;
using Dapper;
using MediatR;

namespace Application.Features.Suppliers.Get;

public class GetSupplierHandler : IRequestHandler<GetSupplierQuery, GetSupplierResponse>
{
    private readonly DbSession _dbSession;
    public GetSupplierHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string GET_QUERY = @"
    SELECT 
          supplier_id AS SupplierId
        , code AS Code
        , name AS Name
        , contact_person AS ContactPerson
        , phone AS Phone
        , tax_code AS TaxCode
        , email AS Email
        , address AS Address
        , created_at AS CreatedAt
        , updated_at AS UpdatedAt
	FROM public.suppliers
    WHERE supplier_id = @SupplierId
    ";

    public async Task<GetSupplierResponse> Handle(GetSupplierQuery request, CancellationToken cancellationToken)
    {
        var row = await _dbSession.Connection.QuerySingleOrDefaultAsync<GetSupplierResponse>(GET_QUERY, new
        {
            SupplierId = request.SupplierId
        });

        if (row == null)
        {
            throw new BusinessException("notfound");
        }

        return row;
    }
}
