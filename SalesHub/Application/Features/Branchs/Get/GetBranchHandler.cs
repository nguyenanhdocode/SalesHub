using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.Branchs.Get;

public class GetBranchHandler : IRequestHandler<GetBranchQuery, BranchDto>
{
    private readonly DbSession _dbSession;
    public GetBranchHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string GET_QUERY = @"
    SELECT 
        branch_id AS BranchId
        , code AS Code
        , name AS Name
        , address AS Address
        , phone AS Phone
        , email AS Email
        , tax_code AS TaxCode
        , created_at AS CreatedAt
        , updated_at AS UpdatedAt
	FROM public.branchs
    WHERE branch_id = @BranchId
    ";

    public async Task<BranchDto> Handle(GetBranchQuery request, CancellationToken cancellationToken)
    {
        var row = await _dbSession.Connection.QuerySingleOrDefaultAsync<BranchDto>(GET_QUERY, new
        {
            BranchId = request.BranchId
        });

        if (row == null)
        {
            throw new BusinessException("notfound");
        }

        return row;
    }
}
