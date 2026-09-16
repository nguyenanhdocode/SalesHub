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

    public async Task<T?> GetColumnValue<T>(string tableName, string pkName, string pkValue, string selectColumn)
    {
        string sql = $"SELECT {selectColumn} FROM {tableName} WHERE CAST({pkName} AS TEXT) = @Value";
        var res = await _dbSession.Connection.QuerySingleOrDefaultAsync<T>(sql, new
        {
            Value = pkValue
        });

        return res;
    }
}
