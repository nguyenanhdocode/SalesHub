using Application.Database;
using Dapper;

namespace Application.Services;

public class QueryService
{
    readonly DbSession _dbSession;
    public QueryService(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string PERIOD_CLOSED_SQL = @"
    SELECT periods.is_closed
    FROM documents
    INNER JOIN periods ON periods.period_id = documents.period_id
    WHERE documents.document_id = @DocumentId;
    ";
    public async Task<bool> IsPeriodClosed(Guid documentId)
    {
        bool isClosed = await _dbSession.Connection.ExecuteScalarAsync<bool>(PERIOD_CLOSED_SQL
        , new
        {
            DocumentId = documentId
        }, _dbSession.Transaction);

        return isClosed;
    }
}
