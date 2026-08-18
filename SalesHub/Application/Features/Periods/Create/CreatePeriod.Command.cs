using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Periods.Create;

public class CreatePeriodCommand : IRequest<int>, ITransactionalRequest
{
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public DateTime FromDate {get;set;}
    public DateTime ToDate {get;set;}

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
