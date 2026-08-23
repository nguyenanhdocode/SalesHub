using System.Security.Cryptography.X509Certificates;
using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.Units.Create;

public class UpdateUnitHandler : IRequestHandler<UpdateUnitCommand>
{
    private readonly DbSession _dbSession;
    public UpdateUnitHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string GET_CODE_BY_ID = @"
    SELECT code FROM units WHERE unit_id = @UnitId
    ";

    private const string CHECK_EXISTS_QUERY = @"
    SELECT EXISTS(SELECT 1 FROM units WHERE code = @Code)
    ";

    private const string UPDATE_QUERY = @"
    UPDATE units SET 
        code = @Code
        , name = @Name
        , active = @Active
        , updated_at = CURRENT_TIMESTAMP
    WHERE unit_id = @UnitId
    ";

    public async Task Handle(UpdateUnitCommand request, CancellationToken cancellationToken)
    {
        string? code = await _dbSession.Connection.ExecuteScalarAsync<string?>(GET_CODE_BY_ID, new
        {
            UnitId = request.UnitId
        });

        if (request.Code != code)
        {
            bool exists = await _dbSession.Connection.ExecuteScalarAsync<bool>(CHECK_EXISTS_QUERY, new
            {
                Code = request.Code
            });

            if (exists)
            {
                throw new BusinessException("exists");
            }
        }

        await _dbSession.Connection.ExecuteAsync(UPDATE_QUERY, request);
    }
}