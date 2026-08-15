using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Warehouses.Create;

public class CreateWarehouseHandler : IRequestHandler<CreateWarehouseCommand, int>
{
    private readonly DbSession _dbSession;
    public CreateWarehouseHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string INSERT_SQL = @"
    INSERT INTO public.warehouses (code, name, branch_id)
	VALUES (@Code, @Name, @BranchId)
    RETURNING warehouse_id;
    ";

    public async Task<int> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(INSERT_SQL, request);

        return id;
    }
}
