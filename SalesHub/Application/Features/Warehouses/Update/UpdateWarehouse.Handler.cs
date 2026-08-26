using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Warehouses.Update;

public class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand>
{
    private readonly DbSession _dbSession;
    public UpdateWarehouseHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string UPDATE_SQL = @"
    UPDATE public.warehouses
	SET code=@Code
    , name=@Name
    , active=@Active
    , updated_at=CURRENT_TIMESTAMP
    , branch_id = @BranchId
	WHERE warehouse_id = @WarehouseId;
    ";

    public async Task Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteScalarAsync<int>(UPDATE_SQL, request);
    }
}
