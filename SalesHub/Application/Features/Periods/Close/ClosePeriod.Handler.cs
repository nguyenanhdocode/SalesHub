using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Periods.Close;

public class ClosePeriodHandler : IRequestHandler<ClosePeriodCommand>
{
    private readonly DbSession _dbSession;
    public ClosePeriodHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string CLOSE_SQL = @"
    UPDATE periods SET is_closed = true WHERE period_id = @PeriodId;
    ";

    public async Task Handle(ClosePeriodCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(CLOSE_SQL, new
        {
            PeriodId = request.PeriodId
        });
    }
}
