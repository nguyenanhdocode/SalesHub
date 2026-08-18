using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.Branchs.Create;

public class CreateBranchHandler : IRequestHandler<CreateBranchCommand, int>
{
    private readonly DbSession _dbSession;

    public CreateBranchHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string INSERT_QUERY = @"
    INSERT INTO public.branchs(
	code, name, address, phone, email, tax_code)
	VALUES (@Code, @Name, @Address, @Phone, @Email, @TaxCode)
    RETURNING branch_id;
    ";

    public async Task<int> Handle(CreateBranchCommand request, CancellationToken cancellationToken)
    {
        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(INSERT_QUERY, request);

        return id;
    }
}
