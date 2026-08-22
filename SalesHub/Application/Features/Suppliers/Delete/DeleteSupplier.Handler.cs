using Application.Database;
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
    ";

    public async Task Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_QUERY, new
        {
            SupplierId = request.SupplierId
        }
        , _dbSession.Transaction);
    }
}
