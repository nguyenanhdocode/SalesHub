using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Branchs.Delete;

public class DeleteBranchHandler : IRequestHandler<DeleteBranchCommand>
{
    private readonly DbSession _dbSession;
    public DeleteBranchHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string DELETE_QUERY = @"
    DELETE FROM public.branchs WHERE branch_id = @BranchId
    ";

    public async Task Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        await _dbSession.Connection.ExecuteAsync(DELETE_QUERY, new
        {
            BranchId = request.BranchId
        });
    }
}
