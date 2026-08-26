using System.Security.Cryptography.X509Certificates;
using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.Units.Create;

public class CreateUnitHandler : IRequestHandler<CreateUnitCommand, int>
{
    private readonly DbSession _dbSession;
    public CreateUnitHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string INSERT_QUERY = @"
    INSERT INTO units (code, name)
    VALUES (@Code, @Name)
    RETURNING unit_id;
    ";

    public async Task<int> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
    {
        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(INSERT_QUERY, new
        {
            Code = request.Code,
            Name = request.Name
        });

        return id;
    }
}
