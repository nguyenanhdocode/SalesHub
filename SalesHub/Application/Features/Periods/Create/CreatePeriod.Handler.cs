using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Periods.Create;

public class CreatePeriodHandler : IRequestHandler<CreatePeriodCommand, int>
{
    private readonly DbSession _dbSession;
    public CreatePeriodHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string INSERT_SQL = @"
    INSERT INTO public.periods(code, name, from_date, to_date, is_closed)
	VALUES (@Code, @Name, @FromDate, @ToDate, false)
    RETURNING period_id;
    ";

    public async Task<int> Handle(CreatePeriodCommand request, CancellationToken cancellationToken)
    {
        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(INSERT_SQL, request);

        return id;
    }
}
