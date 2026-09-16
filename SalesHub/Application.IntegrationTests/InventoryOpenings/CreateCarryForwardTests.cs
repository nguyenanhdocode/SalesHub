using Application.Exceptions;
using Application.Features.GoodsIssues.Create;
using Application.Features.GoodsReceipts.Create;
using Application.Features.InventoryOpenings.CarryForward;
using Application.Features.InventoryOpenings.Create;
using Application.Features.Products.Create;
using Application.Interfaces.Security;
using Application.Models.Documents;
using Application.Shared;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.GoodsIssues;

public class CreateCarryForwardTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public CreateCarryForwardTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Create_Should_Src_Period_Opening()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        int baseUnitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int srcPeriodId = await dataRand.RandomPeriod();
        int dstPeriodId = await dataRand.RandomPeriod();
        int productId1 = await dataRand.RandomProduct(baseUnitId, supplierId);
        int productId2 = await dataRand.RandomProduct(baseUnitId, supplierId);
        await dataRand.InsertProductUnit(productId1, baseUnitId);
        await dataRand.InsertProductUnit(productId2, baseUnitId);
        int branchId = await dataRand.RandomBranch();
        int warehouseId1 = await dataRand.RandomWarehouse(branchId);
        int warehouseId2 = await dataRand.RandomWarehouse(branchId);
        IEnumerable<CreateDocumentResponse> inserts = [];

        try
        {
            var command = new CarryForwardCommand
            {
                SrcPeriodId = srcPeriodId,
                DstPeriodId = dstPeriodId,
                WarehouseIds = [warehouseId1, warehouseId2]
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
                inserts = await sender.Send(command, CancellationToken.None);
            });

            Assert.Equal("src_period_opening", ex.Code);
        }
        finally
        {
            foreach (var inserted in inserts)
            {
                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_opening_lines WHERE document_id = @DocumentId
                ", new { DocumentId = inserted?.DocumentId ?? default });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_openings WHERE document_id = @DocumentId
                ", new { DocumentId = inserted?.DocumentId ?? default });
            }

            await dataRand.DeleteWarehouse(warehouseId1);
            await dataRand.DeleteWarehouse(warehouseId2);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeletePeriod(srcPeriodId);
            await dataRand.DeletePeriod(dstPeriodId);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }

    [Fact]
    public async Task Create_Should_Dst_Period_Closed()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        int baseUnitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int srcPeriodId = await dataRand.RandomPeriod();
        int dstPeriodId = await dataRand.RandomPeriod();
        int productId1 = await dataRand.RandomProduct(baseUnitId, supplierId);
        int productId2 = await dataRand.RandomProduct(baseUnitId, supplierId);
        await dataRand.InsertProductUnit(productId1, baseUnitId);
        await dataRand.InsertProductUnit(productId2, baseUnitId);
        int branchId = await dataRand.RandomBranch();
        int warehouseId1 = await dataRand.RandomWarehouse(branchId);
        int warehouseId2 = await dataRand.RandomWarehouse(branchId);
        IEnumerable<CreateDocumentResponse> inserts = [];

        try
        {
            await dbSession.Connection.ExecuteAsync(@"
            UPDATE periods SET is_closed = true WHERE period_id = @PeriodId
            ", new { PeriodId = srcPeriodId });
            
            await dbSession.Connection.ExecuteAsync(@"
            UPDATE periods SET is_closed = true WHERE period_id = @PeriodId
            ", new { PeriodId = dstPeriodId });

            var command = new CarryForwardCommand
            {
                SrcPeriodId = srcPeriodId,
                DstPeriodId = dstPeriodId,
                WarehouseIds = [warehouseId1, warehouseId2]
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
                inserts = await sender.Send(command, CancellationToken.None);
            });

            Assert.Equal("dst_period_closed", ex.Code);
        }
        finally
        {
            foreach (var inserted in inserts)
            {
                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_opening_lines WHERE document_id = @DocumentId
                ", new { DocumentId = inserted?.DocumentId ?? default });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_openings WHERE document_id = @DocumentId
                ", new { DocumentId = inserted?.DocumentId ?? default });
            }

            await dataRand.DeleteWarehouse(warehouseId1);
            await dataRand.DeleteWarehouse(warehouseId2);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeletePeriod(srcPeriodId);
            await dataRand.DeletePeriod(dstPeriodId);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }

    [Fact]
    public async Task Create_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        int baseUnitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int srcPeriodId = await dataRand.RandomPeriod();
        int dstPeriodId = await dataRand.RandomPeriod();
        int productId1 = await dataRand.RandomProduct(baseUnitId, supplierId);
        int productId2 = await dataRand.RandomProduct(baseUnitId, supplierId);
        await dataRand.InsertProductUnit(productId1, baseUnitId);
        await dataRand.InsertProductUnit(productId2, baseUnitId);
        int branchId = await dataRand.RandomBranch();
        int warehouseId1 = await dataRand.RandomWarehouse(branchId);
        int warehouseId2 = await dataRand.RandomWarehouse(branchId);
        IEnumerable<CreateDocumentResponse> inserts = [];

        try
        {
            await dbSession.Connection.ExecuteAsync(@"
            UPDATE periods SET is_closed = true WHERE period_id = @PeriodId
            ", new { PeriodId = srcPeriodId });

            string insertBalanceSql = @"
            INSERT INTO inventory_balances
            (
                warehouse_id, product_id, unit_id, quantity, amount
            )
            VALUES (@WarehouseId, @ProductId, @UnitId, @Quantity, @Amount)
            ";
            await dbSession.Connection.ExecuteAsync(insertBalanceSql, new
            {
                WarehouseId = warehouseId1,
                ProductId = productId1,
                UnitId = baseUnitId,
                Quantity = 10,
                Amount = 100
            });

            await dbSession.Connection.ExecuteAsync(insertBalanceSql, new
            {
                WarehouseId = warehouseId1,
                ProductId = productId2,
                UnitId = baseUnitId,
                Quantity = 20,
                Amount = 200
            });

            await dbSession.Connection.ExecuteAsync(insertBalanceSql, new
            {
                WarehouseId = warehouseId2,
                ProductId = productId1,
                UnitId = baseUnitId,
                Quantity = 30,
                Amount = 300
            });

            await dbSession.Connection.ExecuteAsync(insertBalanceSql, new
            {
                WarehouseId = warehouseId2,
                ProductId = productId2,
                UnitId = baseUnitId,
                Quantity = 40,
                Amount = 400
            });

            var command = new CarryForwardCommand
            {
                SrcPeriodId = srcPeriodId,
                DstPeriodId = dstPeriodId,
                WarehouseIds = [warehouseId1, warehouseId2]
            };

            inserts = await sender.Send(command, CancellationToken.None);

            Assert.Equal(2, inserts.Count());

            string testMasterSql = @"SELECT COUNT(1)
                FROM inventory_openings WHERE 
                warehouse_id = @WarehouseId AND period_id = @PeriodId";

            var recetip1Count = await dbSession.Connection.ExecuteScalarAsync<int>(testMasterSql
            , new
            {
                WarehouseId = warehouseId1,
                PeriodId = dstPeriodId
            });
            Assert.Equal(1, recetip1Count);

            var recetip2Count = await dbSession.Connection.ExecuteScalarAsync<int>(testMasterSql
            , new
            {
                WarehouseId = warehouseId2,
                PeriodId = dstPeriodId
            });
            Assert.Equal(1, recetip2Count);

            string lineCountSql = @"SELECT COUNT(1)
            FROM inventory_opening_lines
            WHERE product_id = @ProductId
            AND unit_id = @UnitId AND quantity = @Quantity
            AND amount = @Amount";

            int line11Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {                                           
                ProductId = productId1,
                UnitId = baseUnitId,
                Quantity = 10,
                Amount = 100
            });

            Assert.Equal(1, line11Count);

            int line12Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                ProductId = productId2,
                UnitId = baseUnitId,
                Quantity = 20,
                Amount = 200
            });

            Assert.Equal(1, line12Count);

            int line21Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {                                           
                ProductId = productId1,
                UnitId = baseUnitId,
                Quantity = 30,
                Amount = 300
            });

            Assert.Equal(1, line21Count);

            int line22Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                ProductId = productId2,
                UnitId = baseUnitId,
                Quantity = 40,
                Amount = 400
            });

            Assert.Equal(1, line22Count);
        }
        finally
        {
            foreach (var inserted in inserts)
            {
                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_opening_lines WHERE document_id = @DocumentId
                ", new { DocumentId = inserted?.DocumentId ?? default });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_openings WHERE document_id = @DocumentId
                ", new { DocumentId = inserted?.DocumentId ?? default });
            }

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_balances WHERE warehouse_id = ANY(@WarehouseIds);
            ", new { WarehouseIds = new int[] { warehouseId1, warehouseId2 } });

            await dataRand.DeleteWarehouse(warehouseId1);
            await dataRand.DeleteWarehouse(warehouseId2);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeletePeriod(srcPeriodId);
            await dataRand.DeletePeriod(dstPeriodId);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }
}
