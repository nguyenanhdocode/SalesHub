using Application.Database;
using Application.Exceptions;
using Dapper;
using MediatR;

namespace Application.Features.Suppliers.Delete;

public class DeleteSupplierHandler : IRequestHandler<DeleteSupplierCommand>
{
    private readonly DbSession _dbSession;
    public DeleteSupplierHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string DELETE_QUERY = @"
    DELETE FROM public.suppliers WHERE supplier_id = @SupplierId
    RETURNING supplier_id;
    ";

    public async Task Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        int id = await _dbSession.Connection.ExecuteScalarAsync<int>(DELETE_QUERY, new
        {
            SupplierId = request.SupplierId
        }
        , _dbSession.Transaction);

        if (id <= 0)
        {
            throw new BusinessException("notfound");
        }
    }
}
