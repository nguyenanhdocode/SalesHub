using System.Data;
using Application.Database;
using Application.Exceptions;
using Application.Interfaces.Database;
using Application.Interfaces.Security;
using Application.Models.Documents;
using Application.Services;
using Application.Shared;
using Application.Shared.Documents;
using Dapper;
using MediatR;

namespace Application.Features.StockTransfers.Create;

public class CreateStockTransferHandler : IRequestHandler<CreateStockTransferCommand, CreateDocumentResponse>
{
    private readonly DbSession _dbSession;
    private readonly ICurrentUser _currentUser;
    private readonly DocumentNoService _docNoService;

    public CreateStockTransferHandler(DbSession dbSession
        , ICurrentUser currentUser
        , DocumentNoService documentNoService)
    {
        _dbSession = dbSession;
        _currentUser = currentUser;
        _docNoService = documentNoService;
    }

    const string INSERT_MASTER_SQL = @"
    INSERT INTO public.stock_transfers(
	      document_id
        , source_warehouse_id
        , target_warehouse_id
    )
	VALUES (
          @DocumentId
        , @SourceWarehouseId
        , @TargetWarehouseId
    );
    ";

    const string INSERT_LINES_SQL = @"
    WITH lines AS (
        SELECT
            x.document_id, x.product_id, x.src_unit_id, x.dst_unit_id
            , x.src_quantity
            , uc.conversion_factor * x.src_quantity AS dst_quantity
            , x.note
            , NULL AS amount
        FROM jsons_to_recordset(@Lines::jsonb) AS x (
              document_id uuid
            , product_id int
            , src_unit_id
            , dst_unit_id
            , src_quantity
            , note
        )
        INNER JOIN unit_conversions uc ON uc.src_unit_id = x.src_unit_id 
        AND uc.dst_unit_id = x.dst_unit_id AND uc.active = true
    )
    INSERT INTO public.goods_receipt_lines(
	      document_id
        , product_id
        , src_unit_id
        , dst_unit_id
        , src_quantity
        , dst_quantity
        , amount
        , note
    )
	SELECT * FROM lines;
    ";

    const string GET_UNIT_CONVERSIONS_SQL = @"
    SELECT  
        product_id AS ProductId
        , conversion_factor AS ConversionFactor
        , dst_unit_id AS DstUnitId
        , src_unit_id AS SrcUnitId
    FROM unit_conversions 
    WHERE product_id = ANY(@ProductIds) AND active = true
    ";

    public async Task<CreateDocumentResponse> Handle(CreateStockTransferCommand request, CancellationToken cancellationToken)
    {
        // Kiểm tra posting date có nằm trong khoảng của kỳ kế toán hay không
        bool isValidPostingDate = await _dbSession.Connection.ExecuteScalarAsync<bool>(DocumentSqls.CHECK_POSTINGDATE_SQL, new
        {
            PeriodId = request.PeriodId,
            PostingDate = request.PostingDate
        }, _dbSession.Transaction);

        if (!isValidPostingDate)
        {
            throw new BusinessException("invalid_postingdate");
        }

        var id = Guid.CreateVersion7();

        string docNo = request.DocumentNo ?? "";

        if (string.IsNullOrEmpty(docNo))
        {
            docNo = await _docNoService.GetNextDocumentNo("GR", request.DocumentDate.Year, request.DocumentDate.Month);
        }


        // Insert dữ liệu bảng documents
        await _dbSession.Connection.ExecuteAsync(DocumentSqls.INSERT_DOCUMENT_SQL, new CreateDocumentParams
        {
            DocumentId = id
            ,
            DocumentNo = docNo
            ,
            PostingDate = request.PostingDate
            ,
            DocumentDate = request.DocumentDate
            ,
            PeriodId = request.PeriodId
            ,
            DocumentType = DocumentType.NK.ToString()
            ,
            CreatedBy = _currentUser.UserId
            ,
            Note = request.Note
            ,
            Status = request.Status.ToString()
        }, _dbSession.Transaction);

        await _dbSession.Connection.ExecuteAsync(INSERT_MASTER_SQL
        , new
        {
              DocumentId = id
            , SourceWarehouseId = request.SourceWarehouseId
            , TargetWarehouseId = request.TargetWarehouseId
        }, _dbSession.Transaction);

        var unitConversions = await _dbSession.Connection.QueryAsync<UnitConversionRow>(GET_UNIT_CONVERSIONS_SQL
        , new
        {
            ProductIds = request.Lines.Select(p => p.ProductId).ToList()
        }, _dbSession.Transaction);

        var lines = request.Lines.LeftJoin(unitConversions, p => (p.ProductId, p.SrcQuantity, p.DstUnitId)
        , p => (p.ProductId, p.SrcUnitId, p.DstUnitId)
        , (req, uc) => new
        {
              document_id  = id
            , product_id  = req.ProductId
            , src_unit_id = req.SrcUnitId
            , dst_unit_id = req.DstUnitId
            , src_quantity = req.SrcQuantity
            , dst_quantity = Convert.ToInt32(req.SrcQuantity * (uc == null ? 0 : uc.ConversionFactor))
            , note = req.Note
        });

        await _dbSession.Connection.ExecuteAsync(INSERT_LINES_SQL, new
        {
            Lines = lines
        }, _dbSession.Transaction);

        var balances = lines.Select(p => new
        {
            warehouse_id = request.SourceWarehouseId,
            product_id = p.product_id,
            unit_id = p.src_unit_id,
            quantity = -p.src_quantity,
        })
        .Union(
            lines.Select(p => new
            {
                warehouse_id = request.TargetWarehouseId,
                product_id = p.product_id,
                unit_id = p.dst_unit_id,
                quantity = p.dst_quantity,
            }
        ));

        return new CreateDocumentResponse
        {
            DocumentId = id,
            DocumentNo = docNo
        };
    }
}
