using System.Text;
using System.Text.RegularExpressions;
using Application.Database;
using Application.Interfaces.Common;
using Application.Models.Common;
using Dapper;
using MediatR;

namespace Application.Features.Branchs.List;

public class ListBranchHandler : IRequestHandler<ListBranchQuery, PagedResult<BranchListItem>>
{
    private readonly DbSession _dbSession;
    public ListBranchHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private const string FILTER_QUERY = @"
    SELECT 
        branch_id AS BranchId
        , code AS Code
        , name AS Name
        , address AS Address
        , phone AS Phone
        , email AS Email
        , tax_code AS TaxCode
        , created_at AS CreatedAt
        , updated_at AS UpdatedAt
	FROM public.branchs
    WHERE 1=1
    ";

    private const string COUNTER_QUERY = @"
    SELECT COUNT(*) FROM public.branchs
    WHERE 1=1
    ";

    public async Task<PagedResult<BranchListItem>> Handle(ListBranchQuery request, CancellationToken cancellationToken)
    {
        var filterBuilder = new StringBuilder();

        var parameters = new DynamicParameters();

        if (request.BranchId != null)
        {
            filterBuilder.AppendLine("AND branch_id = @BranchId");
            parameters.Add("BranchId", request.BranchId);
        }

        if (!string.IsNullOrEmpty(request.Code))
        {
            filterBuilder.AppendLine("AND code ILIKE @Code");
            parameters.Add("Code", $"%{request.Code}%");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            filterBuilder.AppendLine("AND name ILIKE @Name");
            parameters.Add("Name", $"%{request.Name}%");
        }

        if (!string.IsNullOrEmpty(request.Address))
        {
            filterBuilder.AppendLine("AND address ILIKE @Address");
            parameters.Add("Address", $"%{request.Address}%");
        }

        if (!string.IsNullOrEmpty(request.Phone))
        {
            filterBuilder.AppendLine("AND phone ILIKE @Phone");
            parameters.Add("Phone", $"%{request.Phone}%");
        }

        if (!string.IsNullOrEmpty(request.Email))
        {
            filterBuilder.AppendLine("AND email ILIKE @Email");
            parameters.Add("Email", $"%{request.Email}%");
        }

        if (!string.IsNullOrEmpty(request.TaxCode))
        {
            filterBuilder.AppendLine("AND tax_code ILIKE @TaxCode");
            parameters.Add("TaxCode", $"%{request.TaxCode}%");
        }

        var counterQuery = new StringBuilder(COUNTER_QUERY);
        counterQuery.AppendLine(filterBuilder.ToString());

        int totalRows = await _dbSession.Connection.ExecuteScalarAsync<int>(counterQuery.ToString(), parameters);
        int totalPages = Convert.ToInt32(Math.Ceiling(totalRows / (double)request.PageSize));

        var dataQuery = new StringBuilder(FILTER_QUERY);
        dataQuery.AppendLine(filterBuilder.ToString());
        dataQuery.AppendLine("ORDER BY Code OFFSET @Offset LIMIT @PageSize");
        parameters.Add("Offset", (request.PageNum - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);

        var data = await _dbSession.Connection.QueryAsync<BranchListItem>(dataQuery.ToString(), parameters);

        return new PagedResult<BranchListItem>(data, totalPages, request.PageNum, request.PageSize);
    }
}
