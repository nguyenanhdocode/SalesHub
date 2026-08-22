using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Test;

public class TestDatabaseHandler : IRequestHandler<TestDatabaseCommand, int>
{
    private readonly DbSession _dbSession;
    public TestDatabaseHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public async Task<int> Handle(TestDatabaseCommand request, CancellationToken cancellationToken)
    {
        int value = await _dbSession.Connection.ExecuteScalarAsync<int>("SELECT 1"
            , transaction: _dbSession.Transaction);

        return value;
    }
}
