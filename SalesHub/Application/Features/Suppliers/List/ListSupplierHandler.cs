using System.Text;
using Application.Database;
using Application.Models.Common;
using Dapper;
using MediatR;

namespace Application.Features.Suppliers.List;

public class ListSupplierHandler : IRequestHandler<ListSupplierQuery, PagedResult<SupplierDto>>
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

    public async Task<PagedResult<SupplierDto>> Handle(ListSupplierQuery request, CancellationToken cancellationToken)
    {
        var filterBuilder = new StringBuilder();

            var parameters = new DynamicParameters();

            if (request.SupplierId != null)
            {
                filterBuilder.AppendLine("AND supplier_id = @SupplierId");
                parameters.Add("SupplierId", request.SupplierId);
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

            int totalRows = await _dbSession.Connection.ExecuteScalarAsync<int>(counterQuery.ToString(), parameters);
            int totalPages = Convert.ToInt32(Math.Ceiling(totalRows / (double)request.PageSize));

            var dataQuery = new StringBuilder(FILTER_QUERY);
            dataQuery.AppendLine(filterBuilder.ToString());
            dataQuery.AppendLine("ORDER BY Code OFFSET @Offset LIMIT @PageSize");
            parameters.Add("Offset", (request.PageNum - 1) * request.PageSize);
            parameters.Add("PageSize", request.PageSize);

            var data = await _dbSession.Connection.QueryAsync<SupplierDto>(dataQuery.ToString(), parameters);

            return new PagedResult<SupplierDto>(data, totalPages, request.PageNum, request.PageSize);
    }
}
