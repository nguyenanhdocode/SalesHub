using Application.Exceptions;
using Application.Features.GoodsIssues.Create;
using Application.Features.GoodsIssues.Delete;
using Application.Features.GoodsIssues.Get;
using Application.Features.GoodsIssues.Update;
using Application.Features.GoodsReceipts.Create;
using Application.Features.GoodsReceipts.Delete;
using Application.Features.GoodsReceipts.Get;
using Application.Features.GoodsReceipts.Update;
using Application.Features.Products.Create;
using Application.Interfaces.Security;
using Application.Models.Documents;
using Application.Shared;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.GoodsIssue;

public class GetGoodsIssueTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public GetGoodsIssueTests(ApplicationFixture fixture)
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
    public async Task Get_Should_Success()
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

        var updateCommand = new UpdateGoodsIssueCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddDays(1),
            Reason = "Lý do",
            Status = Shared.DocumentStatus.POSTED,

            Lines = new List<UpdateGoodsIssueLineInput>
            {
                new UpdateGoodsIssueLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    UnitId = baseUnitId
                },
                new UpdateGoodsIssueLineInput
                {
                    ProductId = productId2,
                    DocumentQuantity = 20,
                    ActualQuantity = 20,
                    UnitPrice = 120_000,
                    Note = "Note1",
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

            updateCommand.DocumentId = inserted.DocumentId;
            await sender.Send(updateCommand, CancellationToken.None);

            await sender.Send(new DeleteGoodsIssueCommand { DocumentId = inserted.DocumentId }, CancellationToken.None);

            var res = await sender.Send(new GetGoodsIssueQuery { DocumentId = inserted.DocumentId }
                , CancellationToken.None);
            
            // Master asserts
            Assert.Equal(inserted.DocumentId, res.DocumentId);
            Assert.Equal(inserted.DocumentNo, res.DocumentNo);

            Assert.Equal(updateCommand.DocumentDate.Year, res.DocumentDate.Year);
            Assert.Equal(updateCommand.DocumentDate.Month, res.DocumentDate.Month);
            Assert.Equal(updateCommand.DocumentDate.Day, res.DocumentDate.Day);
            
            Assert.Equal(updateCommand.PostingDate.Year, res.PostingDate.Year);
            Assert.Equal(updateCommand.PostingDate.Month, res.PostingDate.Month);
            Assert.Equal(updateCommand.PostingDate.Day, res.PostingDate.Day);

            int actualPeriodId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT period_id FROM periods WHERE code = @Code AND name = @Name
            ", new { Code = res.PeriodCode,  Name = res.PeriodName});
            Assert.Equal(updateCommand.PeriodId, actualPeriodId);

            Assert.Equal(DateTime.UtcNow.Year, res.CreatedAt.Year);
            Assert.Equal(DateTime.UtcNow.Month, res.CreatedAt.Month);
            Assert.Equal(DateTime.UtcNow.Day, res.CreatedAt.Day);
            Assert.Equal(DateTime.UtcNow.Hour, res.CreatedAt.Hour);

            var actualCreatedUserId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT user_id FROM users WHERE username = @Username
            ", new { Username = res.CreatedUsername});
            Assert.Equal(currentUser.UserId, actualCreatedUserId);

            Assert.Equal(DateTime.UtcNow.Year, res.DeletedAt?.Year);
            Assert.Equal(DateTime.UtcNow.Month, res.DeletedAt?.Month);
            Assert.Equal(DateTime.UtcNow.Day, res.DeletedAt?.Day);
            Assert.Equal(DateTime.UtcNow.Hour, res.DeletedAt?.Hour);

            var actualDeleteUserId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT user_id FROM users WHERE username = @Username
            ", new { Username = res.DeletedUsername});
            Assert.Equal(currentUser.UserId, actualDeleteUserId);

            Assert.Equal(DateTime.UtcNow.Year, res.UpdatedAt?.Year);
            Assert.Equal(DateTime.UtcNow.Month, res.UpdatedAt?.Month);
            Assert.Equal(DateTime.UtcNow.Day, res.UpdatedAt?.Day);
            Assert.Equal(DateTime.UtcNow.Hour, res.UpdatedAt?.Hour);

            var actualUpdatedUserId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT user_id FROM users WHERE username = @Username
            ", new { Username = res.UpdatedUsername});
            Assert.Equal(currentUser.UserId, actualUpdatedUserId);

            Assert.Equal(DocumentStatus.POSTED.ToString(), res.Status);
            Assert.Equal(updateCommand.Reason, res.Reason);

            int actualWarehouseId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT warehouse_id FROM warehouses 
            WHERE code = @Code AND name = @Name
            ", new { Code = res.WarehouseCode,  Name = res.WarehouseName});

            Assert.Equal(command.WarehouseId, actualWarehouseId);

            // Lines asserts
            // Line1
            Assert.Single(res.Lines, p => p.ProductId == updateCommand.Lines[0].ProductId 
                && p.UnitId == updateCommand.Lines[0].UnitId
                && p.ActualQuantity == updateCommand.Lines[0].ActualQuantity
                && p.DocumentQuantity == updateCommand.Lines[0].DocumentQuantity
                && p.Amount == updateCommand.Lines[0].ActualQuantity * updateCommand.Lines[0].UnitPrice
                && p.Note == updateCommand.Lines[0].Note);

            // Line2
            Assert.Single(res.Lines, p => p.ProductId == updateCommand.Lines[1].ProductId 
                && p.UnitId == updateCommand.Lines[1].UnitId
                && p.ActualQuantity == updateCommand.Lines[1].ActualQuantity
                && p.DocumentQuantity == updateCommand.Lines[1].DocumentQuantity
                && p.Amount == updateCommand.Lines[1].ActualQuantity * updateCommand.Lines[1].UnitPrice
                && p.Note == updateCommand.Lines[1].Note);

            foreach (var line in res.Lines)
            {
                int actualProductId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
                SELECT product_id FROM products WHERE internal_code = @Code AND name = @Name;
                ", new { Code = line.ProductInternalCode, Name = line.ProductName });

                Assert.Equal(line.ProductId, actualProductId);

                int actualUnitId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
                SELECT unit_id FROM units WHERE code = @Code AND name = @Name;
                ", new { Code = line.UnitCode, Name = line.UnitName });

                Assert.Equal(line.UnitId, actualUnitId);
            }

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
