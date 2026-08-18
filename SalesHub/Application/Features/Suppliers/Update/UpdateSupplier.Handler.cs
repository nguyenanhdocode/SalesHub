using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;
using Npgsql;

namespace Application.Features.Suppliers.Update;

public class UpdateSupplierHandler : IRequestHandler<UpdateSupplierCommand, int>
{
    private readonly DbSession _dbSession;
    public UpdateSupplierHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string UPDATE_QUERY = @"
    UPDATE public.suppliers
	SET  code=@Code
    , name=@Name
    , contact_person=@ContactPerson
    , phone=@Phone
    , tax_code=@TaxCode
    , email=@Email
    , address=@Address
    , updated_at=CURRENT_TIMESTAMP
	WHERE supplier_id = @SupplierId
    ";

    public async Task<int> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(UPDATE_QUERY, request);

        return id;
    }
}
