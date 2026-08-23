using System.Text;
using Application.Database;
using Application.Models.Common;
using Application.Shared;
using Dapper;
using FluentValidation;
using MediatR;

namespace Application.Features.Units.List;

public class ListUnitHandler : IRequestHandler<ListUnitQuery, PagedResult<UnitListItem>>
{
    private readonly DbSession _dbSession;
    public ListUnitHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string BASE_QUERY = @"
    SELECT
          unit_id AS UnitId
        , code AS Code
        , name AS Name
        , active AS Active
        , created_at AS CreatedAt
        , updated_at AS UpdatedAt
	FROM public.units
    WHERE 1=1
    ";

    private const string COUNT_QUERY = @"SELECT COUNT(1) FROM public.units WHERE 1=1";

    public async Task<PagedResult<UnitListItem>> Handle(ListUnitQuery request, CancellationToken cancellationToken)
    {
        var filterQueryBuilder = new StringBuilder();
        var parameters = new DynamicParameters();

        if (request.UnitId != null)
        {
            filterQueryBuilder.AppendLine(@" AND unit_id = @UnitId");
            parameters.Add("UnitId", request.UnitId);
        }

        if (!string.IsNullOrEmpty(request.Code))
        {
            filterQueryBuilder.AppendLine(@" AND code ILIKE @Code");
            parameters.Add("Code", $"%{request.Code}%");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            filterQueryBuilder.AppendLine(@" AND name ILIKE @Name");
            parameters.Add("Name", $"%{request.Name}%");
        }

        var countQueryBuilder = new StringBuilder(COUNT_QUERY);
        countQueryBuilder.AppendLine(filterQueryBuilder.ToString());

        int totalRows = await _dbSession.Connection.ExecuteScalarAsync<int>(countQueryBuilder.ToString(), parameters);
        int pageSize = request.PageSize > 0 ? request.PageSize : Constants.PAGE_SIZE;

        int totalPages = Convert.ToInt32(Math.Ceiling(totalRows / (double)pageSize));
        
        int pageNum = (request.PageNum > 0 && request.PageNum <= totalPages) ? request.PageNum : 1;

        var dataQueryBuilder = new StringBuilder(BASE_QUERY);
        dataQueryBuilder.AppendLine(filterQueryBuilder.ToString());
        dataQueryBuilder.AppendLine("ORDER BY code OFFSET @Offset LIMIT @PageSize");
        parameters.Add("Offset", (pageNum - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var data = await _dbSession.Connection.QueryAsync<UnitListItem>(dataQueryBuilder.ToString(), parameters);

        return new PagedResult<UnitListItem>(data, totalPages, pageNum, pageSize);
    }
}
