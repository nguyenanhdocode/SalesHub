using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Units.Delete;

public class DeleteUnitHandler : IRequestHandler<DeleteUnitCommand>
{
    public readonly DbSession _dbSession;

    public DeleteUnitHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string DELETE_QUERY = "DELETE FROM units WHERE unit_id = @UnitId";

    public async Task Handle(DeleteUnitCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_QUERY, new
        {
            UnitId = request.UnitId
        });
    }
}
