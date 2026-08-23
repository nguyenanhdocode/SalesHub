using System.Data.Entity.ModelConfiguration.Conventions;
using System.Text;
using Application.Database;
using Application.Models.Common;
using Application.Shared;
using Dapper;
using MediatR;

namespace Application.Features.Suppliers.List;

public class ListSupplierHandler : IRequestHandler<ListSupplierQuery, PagedResult<SupplierListItem>>
{
    private readonly DbSession _dbSession;
    public ListSupplierHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    private readonly string FILTER_QUERY = @"
    SELECT 
          supplier_id AS SupplierId
        , code AS Code
        , name AS Name
        , contact_person AS ContactPerson
        , phone AS Phone
        , tax_code AS TaxCode
        , email AS Email
        , address AS Address
        , created_at AS CreatedAt
        , updated_at AS UpdatedAt
	FROM public.suppliers
    WHERE 1=1
    ";

    private const string COUNTER_QUERY = @"
    SELECT COUNT(*) FROM public.suppliers
    WHERE 1=1
    ";

    public async Task<PagedResult<SupplierListItem>> Handle(ListSupplierQuery request, CancellationToken cancellationToken)
    {
        var filterBuilder = new StringBuilder();

            var parameters = new DynamicParameters();

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

            if (!string.IsNullOrEmpty(request.ContactPerson))
            {
                filterBuilder.AppendLine("AND contact_person ILIKE @ContactPerson");
                parameters.Add("ContactPerson", $"%{request.ContactPerson}%");
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

            int totalRows = await _dbSession.Connection.ExecuteScalarAsync<int>(counterQuery.ToString()
                , parameters
                , _dbSession.Transaction);

            int pageSize = request.PageSize > 0 ? request.PageSize : Constants.PAGE_SIZE;

            int totalPages = Convert.ToInt32(Math.Ceiling(totalRows / (double)pageSize));
            
            int pageNum = (request.PageNum > 0 && request.PageNum <= totalPages) ? request.PageNum : 1;

            var dataQuery = new StringBuilder(FILTER_QUERY);
            dataQuery.AppendLine(filterBuilder.ToString());
            dataQuery.AppendLine("ORDER BY Code OFFSET @Offset LIMIT @PageSize");
            parameters.Add("Offset", (pageNum - 1) * pageSize);
            parameters.Add("PageSize", pageSize);

            var data = await _dbSession.Connection.QueryAsync<SupplierListItem>(dataQuery.ToString()
                , parameters
                , _dbSession.Transaction);

            return new PagedResult<SupplierListItem>(data, totalPages, pageNum, pageSize);
    }
}
