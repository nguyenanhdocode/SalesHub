using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.Units.Get;

public class GetUnitHandler : IRequestHandler<GetUnitQuery, UnitDto>
{
    public readonly DbSession _dbSession;
    public GetUnitHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string GET_QUERY = @"
    SELECT
          unit_id AS UnitId
        , code AS Code
        , name AS Name
        , active AS Active
        , created_at AS CreatedAt
        , updated_at AS UpdatedAt
	FROM public.units
    WHERE unit_id = @UnitId
    ";

    public async Task<UnitDto> Handle(GetUnitQuery request, CancellationToken cancellationToken)
    {
        var row = await _dbSession.Connection.QuerySingleOrDefaultAsync<UnitDto>(GET_QUERY, new
        {
            UnitId = request.UnitId
        });

        if (row == null)
        {
            throw new BusinessException("notfound");
        }

        return row;
    }
}
