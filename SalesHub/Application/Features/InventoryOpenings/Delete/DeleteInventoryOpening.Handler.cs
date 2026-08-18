using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.InventoryOpenings.Delete;

public class DeleteInventoryOpeningHandler : IRequestHandler<DeleteInventoryOpeningCommand>
{
    private readonly DbSession _dbSession;
    public DeleteInventoryOpeningHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string DELETE_MASTER_SQL = @"
    DELETE FROM inventory_openings WHERE document_id = @DocumentId
    ";

    const string DELETE_LINES_SQL = @"
    DELETE FROM inventory_opening_lines WHERE document_id = @DocumentId
    ";

    public async Task Handle(DeleteInventoryOpeningCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_LINES_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);

        await _dbSession.Connection.ExecuteAsync(DELETE_MASTER_SQL, new
        {
            DocumentId = request.DocumentId
        }, _dbSession.Transaction);
    }
}
