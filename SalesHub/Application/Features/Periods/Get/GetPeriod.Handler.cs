using System.Text;
using Application.Database;
using Application.Exceptions;
using Application.Models.Common;
using Dapper;
using MediatR;

namespace Application.Features.Periods.Get;

public class GetPeriodHandler : IRequestHandler<GetPeriodQuery, GetPeriodResponse>
{
    private readonly DbSession _dbSession;
    public GetPeriodHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string GET_SQL = @"
    SELECT 
      period_id AS PeriodId
    , code AS Code
    , name AS Name
    , from_date AS FromDate
    , to_date AS ToDate
    , is_closed AS IsClosed
	FROM public.periods
    WHERE period_id = @PeriodId;
    ";


    public async Task<GetPeriodResponse> Handle(GetPeriodQuery request, CancellationToken cancellationToken)
    {
        var period = await _dbSession.Connection.QueryFirstOrDefaultAsync<GetPeriodResponse>(GET_SQL, new
        {
            PeriodId = request.PeriodId
        });

        if (period == null)
        {
            throw new BusinessException("notfound");
        }

        return period;
    }
}
