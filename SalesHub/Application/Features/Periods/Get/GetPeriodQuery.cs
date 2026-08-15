using System.Data;
using Application.Interfaces.Common;
using Application.Interfaces.Database;
using Application.Models.Common;
using MediatR;

namespace Application.Features.Periods.Get;

public class GetPeriodQuery : IRequest<PeriodDto>, ITransactionalRequest
{
    public int PeriodId { get; set; }

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
