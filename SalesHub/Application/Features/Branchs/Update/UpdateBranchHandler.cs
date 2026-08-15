using Application.Database;
using Application.Exceptions;
using Application.Features.Branchs.Update;
using Dapper;
using MediatR;

namespace Application.Features.Branchs.Create;

public class UpdateBranchHandler : IRequestHandler<UpdateBranchCommand>
{
    private readonly DbSession _dbSession;

    public UpdateBranchHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string UPDATE_QUERY = @"
    UPDATE public.branchs
	SET code=@Code, name=@Name, address=@Address
    , phone=@Phone, email=@Email, tax_code=@TaxCode
    , updated_at = CURRENT_TIMESTAMP
	WHERE branch_id=@BranchId;
    ";

    public async Task Handle(UpdateBranchCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteScalarAsync<int>(UPDATE_QUERY, request);
    }
}
