
using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Periods.Update;

public class UpdatePeriodHandler : IRequestHandler<UpdatePeriodCommand, int>
{
    private readonly DbSession _dbSession;
    public UpdatePeriodHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string UPDATE_SQL = @"
    UPDATE public.periods
	SET code=@Code
    , name=@Name
    , from_date=@FromDate
    , to_date=@ToDate
	WHERE period_id = @PeriodId;
    ";

    public async Task<int> Handle(UpdatePeriodCommand request, CancellationToken cancellationToken)
    {
        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(UPDATE_SQL, request);

        return id;
    }
}
