using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Periods.Delete;

public class DeletePeriodCommand : IRequest, ITransactionalRequest
{
    public int PeriodId {get;set;}

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
