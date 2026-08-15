using System.Security.Cryptography.X509Certificates;
using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.Units.Create;

public class CreateUnitHandler : IRequestHandler<CreateUnitCommand, CreateUnitResponse>
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

    private const string CHECK_EXISTS_QUERY = @"SELECT EXISTS(SELECT 1 FROM units WHERE code = @Code)";

    public async Task<CreateUnitResponse> Handle(CreateUnitCommand request, CancellationToken cancellationToken)
    {
        bool exists = await _dbSession.Connection.QuerySingleAsync<bool>(CHECK_EXISTS_QUERY, new { request.Code });

        if (exists)
        {
            throw new BusinessException("exists");
        }

        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(INSERT_QUERY, new
        {
            Code = request.Code,
            Name = request.Name
        });

        return new CreateUnitResponse
        {
            UnitId = id
        };
    }
}
