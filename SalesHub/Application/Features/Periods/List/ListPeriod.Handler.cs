using System.Text;
using Application.Database;
using Application.Models.Common;
using Application.Shared;
using Dapper;
using MediatR;

namespace Application.Features.Periods.List;

public class ListPeriodHandler : IRequestHandler<ListPeriodQuery, PagedResult<PeriodListItem>>
{
    private readonly DbSession _dbSession;
    public ListPeriodHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string LIST_SQL = @"
    SELECT 
      period_id AS PeriodId
    , code AS Code
    , name AS Name
    , from_date AS FromDate
    , to_date AS ToDate
    , is_closed AS IsClosed
	FROM public.periods
    WHERE 1=1
    ";

    const string COUNTER_SQL = @"
    SELECT COUNT(1) FROM public.periods WHERE 1=1
    ";

    public async Task<PagedResult<PeriodListItem>> Handle(ListPeriodQuery request, CancellationToken cancellationToken)
    {
        var filterQueryBuilder = new StringBuilder();
        var parameters = new DynamicParameters();

        if (request.PeriodId != null)
        {
            filterQueryBuilder.AppendLine(" AND period_id = @PeriodId");
            parameters.Add("PeriodId", request.PeriodId);
        }

        if (!string.IsNullOrEmpty(request.Code))
        {
            filterQueryBuilder.AppendLine(" AND code ILIKE @Code");
            parameters.Add("Code", $"%{request.Code}%");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            filterQueryBuilder.AppendLine(" AND name ILIKE @Name");
            parameters.Add("Name", $"%{request.Name}%");
        }

        if (request.IsClosed != null)
        {
            filterQueryBuilder.AppendLine(" AND is_closed = @IsClosed");
            parameters.Add("IsClosed", request.IsClosed);
        }

        var counterQueryBuilder = new StringBuilder(COUNTER_SQL);
        counterQueryBuilder.AppendLine(filterQueryBuilder.ToString());

        int totalRows = await _dbSession.Connection.ExecuteScalarAsync<int>(counterQueryBuilder.ToString(), parameters);
        int pageSize = request.PageSize > 0 ? request.PageSize : Constants.PAGE_SIZE;
        int totalPages = Convert.ToInt32(Math.Ceiling(totalRows / (double)pageSize));
        int pageNum = (request.PageNum > 0 && request.PageNum <= totalPages) ? request.PageNum : 1;

        var listQueryBuilder = new StringBuilder(LIST_SQL);
        listQueryBuilder.AppendLine(filterQueryBuilder.ToString());
        listQueryBuilder.AppendLine("ORDER BY Code OFFSET @Offset LIMIT @PageSize");
        parameters.Add("Offset", (pageNum - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var data = await _dbSession.Connection.QueryAsync<PeriodListItem>(listQueryBuilder.ToString(), parameters);

        return new PagedResult<PeriodListItem>(data, totalPages, pageNum, pageSize);
    }
}
