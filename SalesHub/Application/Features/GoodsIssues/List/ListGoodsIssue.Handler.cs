using System.Data;
using System.Text;
using Application.Database;
using Application.Interfaces.Database;
using Application.Models.Common;
using Dapper;
using MediatR;

namespace Application.Features.GoodsIssues.List;

public class ListGoodsIssueHandler : IRequestHandler<ListGoodsIssueQuery, PagedResult<GoodsIssueListItem>>
    , ITransactionalRequest
{
    private readonly DbSession _dbSession;
    public ListGoodsIssueHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

    const string BASE_SQL = @"
    SELECT
          documents.document_id AS DocumentId
        , documents.document_no AS DocumentNo
        , documents.posting_date AS PostingDate
        , documents.document_date AS DocumentDate
        , documents.period_id AS PeriodId
        , periods.code AS PeriodCode
        , periods.name AS PeriodName
        , documents.created_at AS CreatedAt
        , users.username AS CreatedUsername
        , documents.deleted_at AS DeletedAt
        , documents.status AS Status
        , goods_issues.reason AS Reason
        , goods_issues.warehouse_id AS WarehouseId
        , warehouses.code AS WarehouseCode
        , warehouses.name AS WarehouseName
        , warehouses.branch_id AS BranchId
        , branchs.code AS BranchCode
        , branchs.name AS BranchName
    FROM documents
    INNER JOIN goods_issues ON goods_issues.document_id = documents.document_id
    INNER JOIN periods ON periods.period_id = documents.period_id
    INNER JOIN users ON users.user_id = documents.created_by
    INNER JOIN warehouses ON warehouses.warehouse_id = goods_issues.warehouse_id
    INNER JOIN branchs ON branchs.branch_id = warehouses.branch_id
    WHERE 1=1
    ";

    const string COUNTER_SQL = @"
    SELECT
        COUNT(1)
    FROM documents
    INNER JOIN goods_issues ON goods_issues.document_id = documents.document_id
    INNER JOIN users ON users.user_id = documents.created_by
    INNER JOIN warehouses ON warehouses.warehouse_id = goods_issues.warehouse_id
    WHERE 1=1
    ";

    public async Task<PagedResult<GoodsIssueListItem>> Handle(ListGoodsIssueQuery request, CancellationToken cancellationToken)
    {
        var filterBuilder = new StringBuilder();

        var parameters = new DynamicParameters();

        if (request.FilterByPeriod)
        {
            if (request.PeriodIds.Count > 0)
            {
                filterBuilder.AppendLine("AND documents.period_id = ANY(@PeriodIds)");
                parameters.Add("PeriodIds", request.PeriodIds);
            }
        }
        else
        {
            if (request.FromDate != null && request.ToDate == null)
            {
                filterBuilder.AppendLine(@"AND (
                    documents.posting_date >= @FromDate OR documents.document_date >= @FromDate
                    OR documents.created_at >= @FromDate
                )");
                parameters.Add("FromDate", request.FromDate);
            }
            else if (request.FromDate == null && request.ToDate != null)
            {
                filterBuilder.AppendLine(@"AND (
                    documents.posting_date <= @ToDate OR documents.document_date <= @ToDate
                    OR documents.created_at <= @ToDate
                )");
                parameters.Add("ToDate", request.ToDate);
            }
            else if (request.FromDate != null && request.ToDate != null)
            {
                filterBuilder.AppendLine(@"AND (
                    documents.posting_date BETWEEN @FromDate AND @ToDate
                    OR documents.document_date BETWEEN @FromDate AND @ToDate
                    OR documents.created_at BETWEEN @FromDate AND @ToDate
                )");
                parameters.Add("FromDate", request.FromDate);
                parameters.Add("ToDate", request.ToDate);
            }
        }

        if (!string.IsNullOrEmpty(request.DocumentNo))
        {
            filterBuilder.AppendLine("AND documents.document_no ILIKE @DocumentNo");
            parameters.Add("DocumentNo", $"%{request.DocumentNo}%");
        }

        if (request.BranchIds.Count > 0)
        {
            filterBuilder.AppendLine("AND warehouses.branch_id = ANY(@BranchIds)");
            parameters.Add("BranchIds", request.BranchIds);
        }

        if (request.WarehouseIds.Count > 0)
        {
            filterBuilder.AppendLine("AND goods_receipts.warehouse_id = ANY(@WarehouseIds)");
            parameters.Add("WarehouseIds", request.WarehouseIds);
        }

        if (!string.IsNullOrEmpty(request.CreatedBy))
        {
            filterBuilder.AppendLine("AND users.username ILIKE @CreatedBy");
            parameters.Add("CreatedBy", $"%{request.CreatedBy}%");
        }

        if (!string.IsNullOrEmpty(request.Reason))
        {
            filterBuilder.AppendLine("AND reason.shipper_name ILIKE @Reason");
            parameters.Add("Reason", $"%{request.Reason}%");
        }

        var counterQuery = new StringBuilder(COUNTER_SQL);
        counterQuery.AppendLine(filterBuilder.ToString());

        int totalRows = await _dbSession.Connection.ExecuteScalarAsync<int>(counterQuery.ToString(), parameters);
        int totalPages = Convert.ToInt32(Math.Ceiling(totalRows / (double)request.PageSize));

        var dataQuery = new StringBuilder(BASE_SQL);
        dataQuery.AppendLine(filterBuilder.ToString());
        dataQuery.AppendLine("ORDER BY CreatedAt OFFSET @Offset LIMIT @PageSize");
        parameters.Add("Offset", (request.PageNum - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);

        var data = await _dbSession.Connection.QueryAsync<GoodsIssueListItem>(dataQuery.ToString(), parameters);

        return new PagedResult<GoodsIssueListItem>(data, totalPages, request.PageNum, request.PageSize);
    }
}
