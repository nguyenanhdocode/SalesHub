using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Periods.Delete;

public class DeletePeriodHandler : IRequestHandler<DeletePeriodCommand>
{
    private readonly DbSession _dbSession;
    public DeletePeriodHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string DELETE_QUERY = @"
    DELETE FROM public.periods WHERE period_id = @PeriodId
    ";

    public async Task Handle(DeletePeriodCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_QUERY, new
        {
            PeriodId = request.PeriodId
        });
    }
}
