using Application.Database;
using Application.Interfaces.Database;
using MediatR;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Application.Behaviors;

public class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly DbSession _session;
    private readonly IConfiguration _configuration;

    public TransactionBehavior(DbSession session
        , IConfiguration configuration)
    {
        _session = session;
        _configuration = configuration;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITransactionalRequest txRequest)
        {
            return await next();
        }

        string dbString = _configuration.GetConnectionString("Default") ?? "";

        var connection = new NpgsqlConnection(dbString);
        connection.Open();
        var transaction = connection.BeginTransaction(txRequest.IsolationLevel);

        _session.Connection = connection;
        _session.Transaction = transaction;

        try
        {
            var response = await next();
            await transaction.CommitAsync(cancellationToken);
            return response;

        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _session.Transaction.DisposeAsync();
            await _session.Connection.CloseAsync();
            await _session.Connection.DisposeAsync();
        }
    }
}