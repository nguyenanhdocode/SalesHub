using Application.Database;
using Dapper;

namespace Application.Services;

public class DocumentNoService
{
    private readonly DbSession _dbSession;
    public DocumentNoService(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string INSERT_SQL = @"

    INSERT INTO document_nos (document_type, document_year, document_month, counter)
    VALUES (@Type, @Year, @Month, 0)
    ON CONFLICT DO NOTHING
    ";

    const string UPDATE_SQL = @"
    WITH next_no AS (
        UPDATE document_nos
        SET counter = counter + 1
        WHERE document_type = @Type AND document_year = @Year AND document_month = @Month
        RETURNING *
    )
    SELECT
        CONCAT(TO_CHAR(counter, 'fm0000')
        , '/'
        , document_year
        , '/'
        , TO_CHAR(document_month, 'fm00')
        , '/'
        , document_type)
    FROM next_no;
    ";

    public async Task<string> GetNextDocumentNo(string type, int year, int month)
    {
        await _dbSession.Connection.ExecuteAsync(INSERT_SQL, new
        {
            Type = type,
            Year = year,
            Month = month
        }, _dbSession.Transaction);

        string? no = await _dbSession.Connection.ExecuteScalarAsync<string?>(UPDATE_SQL, new
        {
            Type = type,
            Year = year,
            Month = month
        }, _dbSession.Transaction);

        if (string.IsNullOrEmpty(no))
        {
            throw new Exception("Can't grenerate document no.");
        }

        return no;
    }
}
