using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Periods.Close;

public class ClosePeriodCommand : IRequest, ITransactionalRequest
{
    public int PeriodId {get;set;}

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
