using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Common;
using MediatR;

namespace Application.Features.Periods.Get;

public class GetPeriodQuery : IRequest<GetPeriodResponse>, ITransactionalRequest
{
    public int PeriodId { get; set; }

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
