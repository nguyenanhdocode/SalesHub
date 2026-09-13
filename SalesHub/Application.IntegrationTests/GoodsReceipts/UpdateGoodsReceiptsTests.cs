using Application.Exceptions;
using Application.Features.GoodsReceipts.Create;
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

namespace Application.IntegrationTests.GoodsReceipts;

public class UpdateGoodsReceiptsTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public UpdateGoodsReceiptsTests(ApplicationFixture fixture)
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

    public static TheoryData<UpdateGoodsReceiptCommand, string> InvalidCommands => new()
    {
        {
            CreateInvalidCommand(command => command.PostingDate = default),
            "PostingDate"
        },
        {
            CreateInvalidCommand(command => command.DocumentDate = default),
            "DocumentDate"
        },
        {
            CreateInvalidCommand(command => command.Note = new string('N', 1001)),
            "Note"
        },
        {
            CreateInvalidCommand(command => command.ShipperName = new string('S', 51)),
            "ShipperName"
        },
        {
            CreateInvalidCommand(command => command.Lines = []),
            "Lines"
        },
        {
            CreateInvalidCommand(command => command.Lines =
            [
                new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                {
                    ProductId = 1,
                    UnitId = 1,
                    DocumentQuantity = 1,
                    ActualQuantity = 1,
                    UnitPrice = 1
                },
                new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                {
                    ProductId = 1,
                    UnitId = 1,
                    DocumentQuantity = 1,
                    ActualQuantity = 1,
                    UnitPrice = 1
                }
            ]),
            "Lines"
        },
        {
            CreateInvalidCommand(command => command.Lines[0].DocumentQuantity = 0),
            "Lines[0].DocumentQuantity"
        },
        {
            CreateInvalidCommand(command => command.Lines[0].ActualQuantity = 0),
            "Lines[0].ActualQuantity"
        },
        {
            CreateInvalidCommand(command => command.Lines[0].UnitPrice = 0),
            "Lines[0].UnitPrice"
        }
    };

    private static UpdateGoodsReceiptCommand CreateInvalidCommand(Action<UpdateGoodsReceiptCommand> configure)
    {
        var command = new UpdateGoodsReceiptCommand
        {
            PostingDate = DateTime.UtcNow,
            DocumentDate = DateTime.UtcNow,
            PeriodId = 1,
            Note = "Note",
            ShipperName = "Nguyễn Văn A",
            Status = DocumentStatus.DRAFT,
            Lines =
            [
                new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                {
                    ProductId = 1,
                    UnitId = 1,
                    DocumentQuantity = 1,
                    ActualQuantity = 1,
                    UnitPrice = 1
                }
            ]
        };

        configure(command);
        return command;
    }

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Update_Should_Throw_Validation_Exception(UpdateGoodsReceiptCommand command, string expectedProperty)
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await sender.Send(command, CancellationToken.None);
        });

        Assert.Contains(exception.Errors, x => x.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task Update_Posted_Should_Success()
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
        int productId3 = await dataRand.RandomProduct(baseUnitId, supplierId);
        await dataRand.InsertProductUnit(productId1, baseUnitId);
        await dataRand.InsertProductUnit(productId2, baseUnitId);
        await dataRand.InsertProductUnit(productId3, baseUnitId);
        int branchId = await dataRand.RandomBranch();
        int warehouseId = await dataRand.RandomWarehouse(branchId);

        var command = new CreateGoodsReceiptCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddDays(1),
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId,

            Lines = new List<Features.GoodsReceipts.Create.GoodsReceiptLineInput>
            {
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                },
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId2,
                    DocumentQuantity = 20,
                    ActualQuantity = 20,
                    UnitPrice = 120_000,
                    Note = "Note1",
                    VatRate = 1.2m,
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted = null;

        try
        {
            inserted = await sender.Send(command, CancellationToken.None);

            var updateCommand = new UpdateGoodsReceiptCommand
            {
                DocumentId = inserted.DocumentId,
                DocumentDate = DateTime.UtcNow.AddDays(1),
                PeriodId = periodId,
                Note = "Note updated",
                PostingDate = DateTime.UtcNow.AddDays(1),
                ShipperName = "Nguyễn Văn AB",
                Status = Shared.DocumentStatus.POSTED,

                Lines = new List<Features.GoodsReceipts.Update.GoodsReceiptLineInput>
                {   
                    // Product 1 -> Update
                    new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                    {
                        ProductId = productId1,
                        DocumentQuantity = 11,
                        ActualQuantity = 11,
                        UnitPrice = 110_000,
                        Note = "Note 1",
                        VatRate = 1.1m,
                        UnitId = baseUnitId
                    },
                    // Product 2 -> Delete
                    // Product 3 -> Update
                    new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                    {
                        ProductId = productId3,
                        DocumentQuantity = 21,
                        ActualQuantity = 21,
                        UnitPrice = 121_000,
                        Note = "Note 2",
                        VatRate = 1.2m,
                        UnitId = baseUnitId
                    }
                }
            };

            await sender.Send(updateCommand, CancellationToken.None);

            // Check goods_receipt_lines
            string lineCountSql = @"SELECT COUNT(1)
            FROM goods_receipt_lines
            WHERE document_id = @DocumentId AND product_id = @ProductId
            AND unit_id = @UnitId AND document_quantity = @DocumentQuantity
            AND actual_quantity = @ActualQuantity
            AND unit_price = @UnitPrice AND vat_rate = @VatRate
            AND note = @Note AND amount = @UnitPrice * @ActualQuantity * @VatRate";

            // Check line of product1 -> should insert
            int line1Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                DocumentQuantity = updateCommand.Lines[0].DocumentQuantity,
                ActualQuantity = updateCommand.Lines[0].ActualQuantity,
                UnitPrice = updateCommand.Lines[0].UnitPrice,
                VatRate = updateCommand.Lines[0].VatRate,
                Note = updateCommand.Lines[0].Note
            });

            Assert.Equal(1, line1Count);

            // Check line of product3 -> should insert
            int line2Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                DocumentQuantity = updateCommand.Lines[1].DocumentQuantity,
                ActualQuantity = updateCommand.Lines[1].ActualQuantity,
                UnitPrice = updateCommand.Lines[1].UnitPrice,
                VatRate = updateCommand.Lines[1].VatRate,
                Note = updateCommand.Lines[1].Note
            });

            // Check line of product2 -> should delete
            int product2Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql
            , new 
            {
                DocumentId = inserted.DocumentId,
                ProductId = productId2,
                DocumentQuantity = 20,
                ActualQuantity = 20,
                UnitPrice = 120_000,
                Note = "Note1",
                VatRate = 1.2m,
                UnitId = baseUnitId
            });
            
            Assert.Equal(0, product2Count);

            // Check balances
            string balanceLineSql = @"
            SELECT COUNT(1) FROM inventory_balances
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId
            AND unit_id = @UnitId AND quantity = @Quantity AND amount = @Amount
            ";

            // Check balance for product1
            int product1BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                Quantity = updateCommand.Lines[0].ActualQuantity,
                Amount = updateCommand.Lines[0].ActualQuantity * updateCommand.Lines[0].UnitPrice * updateCommand.Lines[0].VatRate
            });

            Assert.Equal(1, product1BalanceCount);

            // Check balance for product2
            int product2BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = productId2,
                UnitId = baseUnitId,
                Quantity = 0,
                Amount = 0
            });

            Assert.Equal(1, product2BalanceCount);

            // Check balance for product3
            int product3BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                Quantity = updateCommand.Lines[1].ActualQuantity,
                Amount = updateCommand.Lines[1].ActualQuantity * updateCommand.Lines[1].UnitPrice * updateCommand.Lines[1].VatRate
            });

            Assert.Equal(1, product3BalanceCount);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipts WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dataRand.DeleteDocument(inserted?.DocumentId?? default);

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_balances WHERE warehouse_id = @WarehouseId
            ", new { WarehouseId = warehouseId });

            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeleteProductUnits(productId3);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeleteProduct(productId3);
            await dataRand.DeletePeriod(periodId);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }

    [Fact]
    public async Task Update_Decrease_Quantity_Should_Throw_Insufficient_Inventory()
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

        var command = new CreateGoodsReceiptCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddDays(1),
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId,

            Lines = new List<Features.GoodsReceipts.Create.GoodsReceiptLineInput>
            {
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                },
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId2,
                    DocumentQuantity = 20,
                    ActualQuantity = 20,
                    UnitPrice = 120_000,
                    Note = "Note1",
                    VatRate = 1.2m,
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted = null;

        try
        {
            inserted = await sender.Send(command, CancellationToken.None);

            await dbSession.Connection.ExecuteAsync(@"
            UPDATE inventory_balances
            SET quantity = @Quantity, amount = @Amount
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId
            AND unit_id = @UnitId
            "
            , new
            {
                Quantity = 1,
                Amount = 120_000,
                WarehouseId = warehouseId,
                ProductId = productId2,
                UnitId = baseUnitId
            });

            var updateCommand = new UpdateGoodsReceiptCommand
            {
                DocumentId = inserted.DocumentId,
                DocumentDate = DateTime.UtcNow.AddDays(1),
                PeriodId = periodId,
                Note = "Note updated",
                PostingDate = DateTime.UtcNow.AddDays(1),
                ShipperName = "Nguyễn Văn AB",
                Status = Shared.DocumentStatus.POSTED,

                Lines = new List<Features.GoodsReceipts.Update.GoodsReceiptLineInput>
                {   
                    // Product 1 -> Update
                    new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                    {
                        ProductId = productId1,
                        DocumentQuantity = 11,
                        ActualQuantity = 11,
                        UnitPrice = 110_000,
                        Note = "Note 1",
                        VatRate = 1.1m,
                        UnitId = baseUnitId
                    },
                    // Product 2 -> Update (decrease quantity)
                    new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                    {
                        ProductId = productId2,
                        DocumentQuantity = 10,
                        ActualQuantity = 10,
                        UnitPrice = 121_000,
                        Note = "Note 2",
                        VatRate = 1.2m,
                        UnitId = baseUnitId
                    }
                }
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
               await sender.Send(updateCommand, CancellationToken.None); 
            });

            Assert.Equal("insufficient_inventory", ex.Code);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipts WHERE document_id = @DocumentId
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

    [Fact]
    public async Task Update_Remove_Line_Should_Throw_Insufficient_Inventory()
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

        var command = new CreateGoodsReceiptCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddDays(1),
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId,

            Lines = new List<Features.GoodsReceipts.Create.GoodsReceiptLineInput>
            {
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                },
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId2,
                    DocumentQuantity = 20,
                    ActualQuantity = 20,
                    UnitPrice = 120_000,
                    Note = "Note1",
                    VatRate = 1.2m,
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted = null;

        try
        {
            inserted = await sender.Send(command, CancellationToken.None);

            await dbSession.Connection.ExecuteAsync(@"
            UPDATE inventory_balances
            SET quantity = @Quantity, amount = @Amount
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId
            AND unit_id = @UnitId
            "
            , new
            {
                Quantity = 1,
                Amount = 120_000,
                WarehouseId = warehouseId,
                ProductId = productId2,
                UnitId = baseUnitId
            });

            var updateCommand = new UpdateGoodsReceiptCommand
            {
                DocumentId = inserted.DocumentId,
                DocumentDate = DateTime.UtcNow.AddDays(1),
                PeriodId = periodId,
                Note = "Note updated",
                PostingDate = DateTime.UtcNow.AddDays(1),
                ShipperName = "Nguyễn Văn AB",
                Status = Shared.DocumentStatus.POSTED,

                Lines = new List<Features.GoodsReceipts.Update.GoodsReceiptLineInput>
                {   
                    // Product 1 -> Update
                    new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                    {
                        ProductId = productId1,
                        DocumentQuantity = 11,
                        ActualQuantity = 11,
                        UnitPrice = 110_000,
                        Note = "Note 1",
                        VatRate = 1.1m,
                        UnitId = baseUnitId
                    },
                    // Product 2 -> Remove
                }
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
               await sender.Send(updateCommand, CancellationToken.None); 
            });

            Assert.Equal("insufficient_inventory", ex.Code);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipts WHERE document_id = @DocumentId
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

    [Fact]
    public async Task Update_Draft_To_Posted_Should_Apply_Balances()
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

        var command = new CreateGoodsReceiptCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddDays(1),
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.DRAFT,
            WarehouseId = warehouseId,

            Lines = new List<Features.GoodsReceipts.Create.GoodsReceiptLineInput>
            {
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                },
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId2,
                    DocumentQuantity = 20,
                    ActualQuantity = 20,
                    UnitPrice = 120_000,
                    Note = "Note1",
                    VatRate = 1.2m,
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted = null;

        try
        {
            inserted = await sender.Send(command, CancellationToken.None);

            var updateCommand = new UpdateGoodsReceiptCommand
            {
                DocumentId = inserted.DocumentId,
                DocumentDate = DateTime.UtcNow,
                PeriodId = periodId,
                Note = "Note",
                PostingDate = DateTime.UtcNow.AddDays(1),
                ShipperName = "Nguyễn Văn A",
                Status = Shared.DocumentStatus.POSTED,

                Lines = new List<Features.GoodsReceipts.Update.GoodsReceiptLineInput>
                {
                    new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                    {
                        ProductId = productId1,
                        DocumentQuantity = 11,
                        ActualQuantity = 11,
                        UnitPrice = 110_000,
                        Note = "Note 1",
                        VatRate = 1.1m,
                        UnitId = baseUnitId
                    },
                    new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                    {
                        ProductId = productId2,
                        DocumentQuantity = 21,
                        ActualQuantity = 21,
                        UnitPrice = 121_000,
                        Note = "Note 2",
                        VatRate = 1.2m,
                        UnitId = baseUnitId
                    }
                }
            };

            await sender.Send(updateCommand, CancellationToken.None);

            // Check inventory_balances table -> must insert
            string balanceLineSql = @"
            SELECT COUNT(1) FROM inventory_balances
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId
            AND unit_id = @UnitId AND quantity = @Quantity AND amount = @Amount
            ";

            int balanceLine1Count = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                Quantity = updateCommand.Lines[0].ActualQuantity,
                Amount = updateCommand.Lines[0].ActualQuantity * updateCommand.Lines[0].UnitPrice * updateCommand.Lines[0].VatRate
            });

            Assert.Equal(1, balanceLine1Count);

            int balanceLine2Count = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                Quantity = updateCommand.Lines[1].ActualQuantity,
                Amount = updateCommand.Lines[1].ActualQuantity * updateCommand.Lines[1].UnitPrice * updateCommand.Lines[1].VatRate
            });

            Assert.Equal(1, balanceLine2Count);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipts WHERE document_id = @DocumentId
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

    [Fact]
    public async Task Update_Should_Throw_Invalid_PostingDate()
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

        var command = new CreateGoodsReceiptCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow,
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId,

            Lines = new List<Features.GoodsReceipts.Create.GoodsReceiptLineInput>
            {
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted = null;

        try
        {
            inserted = await sender.Send(command, CancellationToken.None);

            var updateCommand = new UpdateGoodsReceiptCommand
            {
                DocumentDate = DateTime.UtcNow,
                PeriodId = periodId,
                Note = "Note",
                PostingDate = DateTime.UtcNow.AddYears(1),
                ShipperName = "Nguyễn Văn A",
                Status = Shared.DocumentStatus.POSTED,

                Lines = new List<Features.GoodsReceipts.Update.GoodsReceiptLineInput>
                {
                    new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                    {
                        ProductId = productId1,
                        DocumentQuantity = 10,
                        ActualQuantity = 10,
                        UnitPrice = 100_000,
                        Note = "Note1",
                        VatRate = 1.1m,
                        UnitId = baseUnitId
                    }
                }
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
                await sender.Send(updateCommand, CancellationToken.None);
            });

            Assert.Equal("invalid_postingdate", ex.Code);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipts WHERE document_id = @DocumentId
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

    [Fact]
    public async Task Update_Should_Throw_Period_Closed()
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

        var command = new CreateGoodsReceiptCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow,
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId,

            Lines = new List<Features.GoodsReceipts.Create.GoodsReceiptLineInput>
            {
                new Features.GoodsReceipts.Create.GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted = null;

        try
        {
            inserted = await sender.Send(command, CancellationToken.None);

            await dbSession.Connection.ExecuteAsync(@"
            UPDATE periods SET is_closed = true WHERE period_id = @PeriodId
            ", new { PeriodId = periodId }); 

            var updateCommand = new UpdateGoodsReceiptCommand
            {
                DocumentId = inserted.DocumentId,
                DocumentDate = DateTime.UtcNow,
                PeriodId = periodId,
                Note = "Note",
                PostingDate = DateTime.UtcNow,
                ShipperName = "Nguyễn Văn A",
                Status = Shared.DocumentStatus.POSTED,

                Lines = new List<Features.GoodsReceipts.Update.GoodsReceiptLineInput>
                {
                    new Features.GoodsReceipts.Update.GoodsReceiptLineInput
                    {
                        ProductId = productId1,
                        DocumentQuantity = 10,
                        ActualQuantity = 10,
                        UnitPrice = 100_000,
                        Note = "Note1",
                        VatRate = 1.1m,
                        UnitId = baseUnitId
                    }
                }
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
                await sender.Send(updateCommand, CancellationToken.None);
            });

            Assert.Equal("period_closed", ex.Code);
        }
        finally
        {
             await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipts WHERE document_id = @DocumentId
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
