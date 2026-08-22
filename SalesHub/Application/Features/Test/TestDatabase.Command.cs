using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Test;

public class TestDatabaseCommand : IRequest<int>, ITransactionalRequest
{
    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
