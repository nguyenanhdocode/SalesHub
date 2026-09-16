using Application.Exceptions;
using Application.Features.GoodsIssues.Create;
using Application.Features.GoodsReceipts.Create;
using Application.Features.InventoryOpenings.Create;
using Application.Features.InventoryOpenings.Get;
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

public class GetInventoryOpeningTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public GetInventoryOpeningTests(ApplicationFixture fixture)
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

            var res = await sender.Send(new GetInventoryOpeningQuery { DocumentId = inserted.DocumentId }
                , CancellationToken.None);

            Assert.Equal(inserted.DocumentId, res.DocumentId);
            Assert.Equal(inserted.DocumentNo, res.DocumentNo);

            int actualBranchId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT branch_id FROM branchs WHERE code = @Code AND name = @Name
            ", new { Code = res.BranchCode, Name = res.BranchName });
            Assert.Equal(res.BranchId, actualBranchId);

            int actualWarehouseId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT warehouse_id FROM warehouses WHERE code = @Code AND name = @Name
            ", new { Code = res.WarehouseCode, Name = res.WarehouseName });
            Assert.Equal(res.WarehouseId, actualWarehouseId);

            int actualPeriodId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT period_id FROM periods WHERE code = @Code AND name = @Name
            ", new { Code = res.PeriodCode, Name = res.PeriodName });
            Assert.Equal(res.PeriodId, actualPeriodId);

            var actualCreatedById = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT user_id FROM users WHERE username = @Username
            ", new { Username = res.CreatedUserName });
            Assert.Equal(res.CreatedBy, actualCreatedById);

            Assert.Equal(DateTime.UtcNow.Year, res.CreatedAt.Year);
            Assert.Equal(DateTime.UtcNow.Month, res.CreatedAt.Month);
            Assert.Equal(DateTime.UtcNow.Day, res.CreatedAt.Day);
            Assert.Equal(DateTime.UtcNow.Hour, res.CreatedAt.Hour);
            Assert.Equal(DateTime.UtcNow.Minute, res.CreatedAt.Minute);

            var actualUpdatedById = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT user_id FROM users WHERE username = @Username
            ", new { Username = res.UpdatedUserName });
            Assert.Equal(res.UpdatedBy, actualUpdatedById);

            Assert.Equal(DateTime.UtcNow.Year, res.UpdatedAt?.Year);
            Assert.Equal(DateTime.UtcNow.Month, res.UpdatedAt?.Month);
            Assert.Equal(DateTime.UtcNow.Day, res.UpdatedAt?.Day);
            Assert.Equal(DateTime.UtcNow.Hour, res.UpdatedAt?.Hour);
            Assert.Equal(DateTime.UtcNow.Minute, res.UpdatedAt?.Minute);

            Assert.Equal(updateCommand.Note, res.Note);

            int product1LineCount = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM inventory_opening_lines
            WHERE product_id = @ProductId AND unit_id = @UnitId AND quantity = @Quantity
            AND amount = @Amount
            ", updateCommand.Lines[0]);
            Assert.Equal(1, product1LineCount);

            int product2LineCount = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM inventory_opening_lines
            WHERE product_id = @ProductId AND unit_id = @UnitId AND quantity = @Quantity
            AND amount = @Amount
            ", updateCommand.Lines[1]);
            Assert.Equal(1, product2LineCount);
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
