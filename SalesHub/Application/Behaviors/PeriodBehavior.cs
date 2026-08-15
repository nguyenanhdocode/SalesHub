using System.Text;
using Application.Database;
using Application.Exceptions;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Dapper;
using MediatR;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Application.Behaviors;

public class PeriodBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly DbSession _session;


    public PeriodBehavior(DbSession session)
    {
        _session = session;
    }

    const string CHECK_CLOSED_SQL = @"
    SELECT EXISTS(SELECT 1 FROM periods WHERE period_id = @PeriodId AND is_closed = true)
    ";

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is ICheckPeriodForCreateRequest createRequest)
        {
            bool isClosed = await _session.Connection.ExecuteScalarAsync<bool>(CHECK_CLOSED_SQL, new
            {
                PeriodId = createRequest.PeriodId
            }, _session.Transaction);

            if (isClosed)
            {
                throw new BusinessException("period_closed");
            }
        }
        else if (request is ICheckPeriodForUpdateRequest updateRequest)
        {
            string sql = @$"
            SELECT EXISTS(SELECT 1
            FROM {updateRequest.TableName}
            INNER JOIN periods ON periods.period_id = {updateRequest.TableName}.period_id AND periods.is_closed = true
            WHERE {updateRequest.TableName}.{updateRequest.PkName} = @PkValue)
            ";

            bool isClosed = await _session.Connection.ExecuteScalarAsync<bool>(sql, new
            {
               PkValue = updateRequest.PkValue 
            }, _session.Transaction);

            if (isClosed)
            {
                throw new BusinessException("period_closed");
            }
        }

        return await next();
    }
}