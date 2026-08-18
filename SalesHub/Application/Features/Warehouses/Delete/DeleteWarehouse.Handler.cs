using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Warehouses.Delete;

public class DeleteWarehouseHandler : IRequestHandler<DeleteWarehouseCommand>
{
    private readonly DbSession _dbSession;
    public DeleteWarehouseHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string DELETE_QUERY = @"
    DELETE FROM public.warehouses WHERE warehouse_id = @WarehouseId
    ";

    public async Task Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_QUERY, new
        {
            WarehouseId = request.WarehouseId
        });
    }
}
