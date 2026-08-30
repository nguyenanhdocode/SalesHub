using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Periods.Update;

public class UpdatePeriodCommand : IRequest<int>, ITransactionalRequest
{
    public int PeriodId {get;set;}
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public DateTime FromDate {get;set;}
    public DateTime ToDate {get;set;}

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
