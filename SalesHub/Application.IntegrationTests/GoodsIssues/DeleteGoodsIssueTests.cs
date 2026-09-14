using Application.Exceptions;
using Application.Features.GoodsIssues.Create;
using Application.Features.GoodsIssues.Delete;
using Application.Features.GoodsReceipts.Create;
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

public class DeleteGoodsIssueTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public DeleteGoodsIssueTests(ApplicationFixture fixture)
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
    public async Task Delete_Should_Success()
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

        var command = new CreateGoodsIssueCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddDays(1),
            Reason = "Lý do",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId,

            Lines = new List<CreateGoodsIssueLineInput>
            {
                new CreateGoodsIssueLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    UnitId = baseUnitId
                },
                new CreateGoodsIssueLineInput
                {
                    ProductId = productId2,
                    DocumentQuantity = 20,
                    ActualQuantity = 20,
                    UnitPrice = 120_000,
                    Note = "Note2",
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted = null;

        try
        {
            string insertBalanceSql = @"
            INSERT INTO inventory_balances (warehouse_id, product_id, unit_id, quantity, amount)
            VALUES (@WarehouseId, @ProductId, @UnitId, @Quantity, @Amount);
            ";

            await dbSession.Connection.ExecuteAsync(insertBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = command.Lines[0].ProductId,
                UnitId = command.Lines[0].UnitId,
                Quantity = command.Lines[0].ActualQuantity,
                Amount = command.Lines[0].ActualQuantity * command.Lines[0].UnitPrice,
            });

            await dbSession.Connection.ExecuteAsync(insertBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = command.Lines[1].ProductId,
                UnitId = command.Lines[1].UnitId,
                Quantity = command.Lines[1].ActualQuantity,
                Amount = command.Lines[1].ActualQuantity * command.Lines[1].UnitPrice,
            });

            inserted = await sender.Send(command, CancellationToken.None);

            await sender.Send(new DeleteGoodsIssueCommand { DocumentId = inserted.DocumentId }, CancellationToken.None);

            string testBalanceSql = @"
            SELECT COUNT(1) FROM inventory_balances
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId AND unit_id = @UnitId
            AND quantity = @Quantity AND amount = @Amount;
            ";

            int line1BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(testBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = command.Lines[0].ProductId,
                UnitId = command.Lines[0].UnitId,
                Quantity = command.Lines[0].ActualQuantity,
                Amount = command.Lines[0].ActualQuantity * command.Lines[0].UnitPrice,
            });

            Assert.Equal(1, line1BalanceCount);

            int line2BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(testBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = command.Lines[1].ProductId,
                UnitId = command.Lines[1].UnitId,
                Quantity = command.Lines[1].ActualQuantity,
                Amount = command.Lines[1].ActualQuantity * command.Lines[1].UnitPrice,
            });

            Assert.Equal(1, line2BalanceCount);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_issue_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_issues WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dataRand.DeleteDocument(inserted?.DocumentId?? default);

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