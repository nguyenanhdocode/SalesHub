using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;
using Npgsql;

namespace Application.Features.Suppliers.Create;

public class CreateSupplierHandler : IRequestHandler<CreateSupplierCommand, int>
{
    private readonly DbSession _dbSession;
    public CreateSupplierHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string INSERT_QUERY = @"
    INSERT INTO public.suppliers(
          code
        , name
        , contact_person
        , phone
        , tax_code
        , email
        , address)
	VALUES (@Code, @Name, @ContactPerson, @Phone, @TaxCode, @Email, @Address)
    RETURNING supplier_id;
    ";

    public async Task<int> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(INSERT_QUERY, request, _dbSession.Transaction);
        return id;
    }
}
