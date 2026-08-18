using System.Collections.Immutable;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Application.Database;
using Dapper;
using MediatR;

namespace Application.Features.Products.UnitConversions.Update;

public class UpdateUnitConversionsHandler : IRequestHandler<UpdateUnitConversionsCommand>
{
    private readonly DbSession _dbSession;
    public UpdateUnitConversionsHandler(DbSession dbSession)
    {
        _dbSession = dbSession;
    }

    const string GET_UNIT_CONVERSIONS_BY_PRODUCT_ID_QUERY = @"
    SELECT
        product_id AS ProductId 
        , src_unit_id AS SrcUnitId
        , dst_unit_id AS DstUnitId
    FROM unit_conversions
    WHERE product_id = @ProductId;
    ";

    const string DELETE_SQL = @"
    DELETE FROM unit_conversions 
    WHERE product_id = @ProductId AND src_unit_id = @SrcUnitId AND dst_unit_id = @DstUnitId; 
    ";

    const string INSERT_SQL = @"
    INSERT INTO unit_conversions (product_id, src_unit_id, dst_unit_id, conversion_factor)
    VALUES (@ProductId, @SrcUnitId, @DstUnitId, @ConversionFactor);
    ";

    const string UPDATE_SQL = @"
    UPDATE unit_conversions SET conversion_factor = @ConversionFactor
    WHERE product_id = @ProductId AND src_unit_id = @SrcUnitId AND dst_unit_id = @DstUnitId;
    ";

    public async Task Handle(UpdateUnitConversionsCommand request, CancellationToken cancellationToken)
    {
        var dbUnits = await _dbSession.Connection.QueryAsync<UnitConversionInput>(GET_UNIT_CONVERSIONS_BY_PRODUCT_ID_QUERY
                        , new
                        {
                            ProductId = request.ProductId
                        });

        // Delete
        var deleteUnits = dbUnits.ExceptBy(request.Conversions.Select(p => (p.SrcUnitId, p.DstUnitId))
            , p => (p.SrcUnitId, p.DstUnitId))
            .Select(p => new
            {
                ProductId = request.ProductId,
                SrcUnitId = p.SrcUnitId,
                DstUnitId = p.DstUnitId
            }).ToList();

        if (deleteUnits.Count > 0)
        {
            await _dbSession.Connection.ExecuteAsync(DELETE_SQL, deleteUnits, _dbSession.Transaction);
        }

        // Insert
        var insertUnits = request.Conversions.ExceptBy(dbUnits.Select(p => (p.SrcUnitId, p.DstUnitId))
            , p => (p.SrcUnitId, p.DstUnitId))
            .Select(p => new
            {
                ProductId = request.ProductId,
                SrcUnitId = p.SrcUnitId,
                DstUnitId = p.DstUnitId,
                ConversionFactor = p.ConversionFactor
            }).ToList();

        if (insertUnits.Count > 0)
        {
            await _dbSession.Connection.ExecuteAsync(INSERT_SQL, insertUnits, _dbSession.Transaction);
        }

        // Update
        var updateUnits = request.Conversions
            .Where(p => dbUnits.Any(x => x.SrcUnitId == p.SrcUnitId && x.DstUnitId == p.DstUnitId))
            .Select(p => new
            {
                ProductId = request.ProductId,
                SrcUnitId = p.SrcUnitId,
                DstUnitId = p.DstUnitId,
                ConversionFactor = p.ConversionFactor
            }).ToList();

        if (updateUnits.Count > 0)
        {
            await _dbSession.Connection.ExecuteAsync(UPDATE_SQL, updateUnits, _dbSession.Transaction);
        }
    }
}
