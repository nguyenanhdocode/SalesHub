using Application.Exceptions;
using Application.Features.GoodsIssues.Create;
using Application.Features.GoodsReceipts.Create;
using Application.Features.InventoryOpenings.Create;
using Application.Features.InventoryOpenings.Update;
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

public class UpdateInventoryOpeningTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public UpdateInventoryOpeningTests(ApplicationFixture fixture)
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
    public async Task Create_Should_Not_Update_Balances()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        int baseUnitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int periodId = await dataRand.RandomPeriod();
        int productId1 = await dataRand.RandomProduct(baseUnitId, supplierId);
        int productId2 = await dataRand.RandomProduct(baseUnitId, supplierId);
        await dataRand.InsertProductUnit(productId1, baseUnitId);
        await dataRand.InsertProductUnit(productId2, baseUnitId);
        int branchId = await dataRand.RandomBranch();
        int warehouseId = await dataRand.RandomWarehouse(branchId);

        var command = new CreateInventoryOpeningCommand
        {
            PeriodId = periodId,
            Note = "Note",
            WarehouseId = warehouseId,
            WriteToBalances = false,

            Lines = new List<CreateInventoryOpeningLineInput>
            {
                new CreateInventoryOpeningLineInput
                {
                    ProductId = productId1,
                    Quantity = 10,
                    Amount = 100_000,
                    UnitId = baseUnitId
                },
                new CreateInventoryOpeningLineInput
                {
                    ProductId = productId2,
                    Quantity = 20,
                    Amount = 200_000,
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted = null;

        try
        {
            inserted = await sender.Send(command, CancellationToken.None);

            var updateCommand = new UpdateInventoryOpeningCommand
            {
                DocumentId = inserted.DocumentId,
                Note = "Note updated",
                WriteToBalances = false,

                Lines = new List<UpdateInventoryOpeningLineInput>
                {
                    new UpdateInventoryOpeningLineInput
                    {
                        ProductId = productId1,
                        Quantity = 11,
                        Amount = 110_000,
                        UnitId = baseUnitId
                    },
                    new UpdateInventoryOpeningLineInput
                    {
                        ProductId = productId2,
                        Quantity = 21,
                        Amount = 210_000,
                        UnitId = baseUnitId
                    }
                }
            };

            await sender.Send(updateCommand, CancellationToken.None);

            // Check goods_issues table -> should inserted
            var actualReceiptId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT document_id
            FROM inventory_openings WHERE 
            warehouse_id = @WarehouseId AND period_id = @PeriodId
            "
            , new
            {
                WarehouseId = warehouseId,
                PeriodId = periodId
            });

            Assert.Equal(inserted.DocumentId, actualReceiptId);

            string lineCountSql = @"SELECT COUNT(1)
            FROM inventory_opening_lines
            WHERE document_id = @DocumentId AND product_id = @ProductId
            AND unit_id = @UnitId AND quantity = @Quantity
            AND amount = @Amount";

            // Check line 1 -> should insert
            int line1Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                Quantity = updateCommand.Lines[0].Quantity,
                Amount = updateCommand.Lines[0].Amount
            });

            Assert.Equal(1, line1Count);

            // Check line 2 -> should insert
            int line2Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                Quantity = updateCommand.Lines[1].Quantity,
                Amount = updateCommand.Lines[1].Amount
            });

            Assert.Equal(1, line2Count);

            // Check balances -> should not insert
            int balanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM inventory_balances WHERE warehouse_id = @WarehouseId
            ", new { WarehouseId = warehouseId });

            Assert.Equal(0, balanceCount);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_opening_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_openings WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_balances WHERE warehouse_id = @WarehouseId
            ", new { WarehouseId = warehouseId });

            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeletePeriod(periodId);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }

    [Fact]
    public async Task Create_Should_Update_Balances()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        int baseUnitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int periodId = await dataRand.RandomPeriod();
        int productId1 = await dataRand.RandomProduct(baseUnitId, supplierId);
        int productId2 = await dataRand.RandomProduct(baseUnitId, supplierId);
        await dataRand.InsertProductUnit(productId1, baseUnitId);
        await dataRand.InsertProductUnit(productId2, baseUnitId);
        int branchId = await dataRand.RandomBranch();
        int warehouseId = await dataRand.RandomWarehouse(branchId);

        var command = new CreateInventoryOpeningCommand
        {
            PeriodId = periodId,
            Note = "Note",
            WarehouseId = warehouseId,
            WriteToBalances = true,

            Lines = new List<CreateInventoryOpeningLineInput>
            {
                new CreateInventoryOpeningLineInput
                {
                    ProductId = productId1,
                    Quantity = 10,
                    Amount = 100_000,
                    UnitId = baseUnitId
                },
                new CreateInventoryOpeningLineInput
                {
                    ProductId = productId2,
                    Quantity = 20,
                    Amount = 200_000,
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted = null;

        try
        {
            inserted = await sender.Send(command, CancellationToken.None);

            var updateCommand = new UpdateInventoryOpeningCommand
            {
                DocumentId = inserted.DocumentId,
                Note = "Note updated",
                WriteToBalances = true,

                Lines = new List<UpdateInventoryOpeningLineInput>
                {
                    new UpdateInventoryOpeningLineInput
                    {
                        ProductId = productId1,
                        Quantity = 11,
                        Amount = 110_000,
                        UnitId = baseUnitId
                    },
                    new UpdateInventoryOpeningLineInput
                    {
                        ProductId = productId2,
                        Quantity = 21,
                        Amount = 210_000,
                        UnitId = baseUnitId
                    }
                }
            };

            await sender.Send(updateCommand, CancellationToken.None);

            // Check goods_issues table -> should inserted
            var actualReceiptId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT document_id
            FROM inventory_openings WHERE 
            warehouse_id = @WarehouseId AND period_id = @PeriodId
            "
            , new
            {
                WarehouseId = warehouseId,
                PeriodId = periodId
            });

            Assert.Equal(inserted.DocumentId, actualReceiptId);

            string lineCountSql = @"SELECT COUNT(1)
            FROM inventory_opening_lines
            WHERE document_id = @DocumentId AND product_id = @ProductId
            AND unit_id = @UnitId AND quantity = @Quantity
            AND amount = @Amount";

            // Check line 1 -> should insert
            int line1Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                Quantity = updateCommand.Lines[0].Quantity,
                Amount = updateCommand.Lines[0].Amount
            });

            Assert.Equal(1, line1Count);

            // Check line 2 -> should insert
            int line2Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                Quantity = updateCommand.Lines[1].Quantity,
                Amount = updateCommand.Lines[1].Amount
            });

            Assert.Equal(1, line2Count);

            string checkBalanceSql = @"
            SELECT COUNT(1) FROM inventory_balances WHERE warehouse_id = @WarehouseId
            AND product_id = @ProductId AND unit_id = @UnitId AND quantity = @Quantity
            AND amount = @Amount
            ";

            // Check balances -> should not insert
            // Product1 balance
            int prodcut1BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(checkBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[0].ProductId,                                                           
                UnitId = updateCommand.Lines[0].UnitId,                                                           
                Quantity = updateCommand.Lines[0].Quantity,                                                           
                Amount = updateCommand.Lines[0].Amount,                                                           
            });

            Assert.Equal(1, prodcut1BalanceCount);

            // Product1 balance
            int prodcut2BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(checkBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[1].ProductId,                                                           
                UnitId = updateCommand.Lines[1].UnitId,                                                           
                Quantity = updateCommand.Lines[1].Quantity,                                                           
                Amount = updateCommand.Lines[1].Amount,                                                           
            });

            Assert.Equal(1, prodcut2BalanceCount);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_opening_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_openings WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_balances WHERE warehouse_id = @WarehouseId
            ", new { WarehouseId = warehouseId });

            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeletePeriod(periodId);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }
}
