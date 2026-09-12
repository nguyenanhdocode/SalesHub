using Application.Exceptions;
using Application.Features.GoodsReceipts.Create;
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

public class CreateGoodsReceiptsTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public CreateGoodsReceiptsTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<CreateGoodsReceiptCommand, string> InvalidCommands => new()
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
                new GoodsReceiptLineInput
                {
                    ProductId = 1,
                    UnitId = 1,
                    DocumentQuantity = 1,
                    ActualQuantity = 1,
                    UnitPrice = 1
                },
                new GoodsReceiptLineInput
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

    private static CreateGoodsReceiptCommand CreateInvalidCommand(Action<CreateGoodsReceiptCommand> configure)
    {
        var command = new CreateGoodsReceiptCommand
        {
            PostingDate = DateTime.UtcNow,
            DocumentDate = DateTime.UtcNow,
            PeriodId = 1,
            Note = "Note",
            ShipperName = "Nguyễn Văn A",
            Status = DocumentStatus.DRAFT,
            WarehouseId = 1,
            Lines =
            [
                new GoodsReceiptLineInput
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
    public async Task Create_Should_Throw_Validation_Exception(CreateGoodsReceiptCommand command, string expectedProperty)
    {
        using var scope = _fixture.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await sender.Send(command, CancellationToken.None);
        });

        Assert.Contains(exception.Errors, x => x.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task Create_Draft_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

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

            Lines = new List<GoodsReceiptLineInput>
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                },
                new GoodsReceiptLineInput
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

            // Check documents table -> should inserted
            var actualDocId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT document_id FROM documents
            WHERE document_no = @DocumentNo 
            AND posting_date = @PostingDate
            AND document_date = @DocumentDate 
            AND period_id = @PeriodId
            AND document_type = @DocumentType AND created_by = @CreatedBy
            AND note = @Note AND deleted_by IS NULL AND deleted_at IS NULL
            AND deleted_reason IS NULL AND status = @Status
            "
            , new
            {
                DocumentNo = inserted.DocumentNo,
                PostingDate = command.PostingDate,
                DocumentDate = command.DocumentDate,
                PeriodId = periodId,
                DocumentType = DocumentType.NK.ToString(),
                CreatedBy = currentUser.UserId,
                Note = command.Note,
                Status = DocumentStatus.DRAFT.ToString()
            });

            Assert.Equal(inserted.DocumentId, actualDocId);

            // Check goods_receipts table -> should inserted
            var actualReceiptId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT document_id
            FROM goods_receipts WHERE 
            warehouse_id = @WarehouseId AND shipper_name = @ShipperName
            AND document_id = @DocumentId
            "
            , new
            {
                WarehouseId = warehouseId,
                ShipperName = command.ShipperName,
                DocumentId = inserted.DocumentId
            });

            Assert.Equal(inserted.DocumentId, actualReceiptId);

            string lineCountSql = @"SELECT COUNT(1)
            FROM goods_receipt_lines
            WHERE document_id = @DocumentId AND product_id = @ProductId
            AND unit_id = @UnitId AND document_quantity = @DocumentQuantity
            AND actual_quantity = @ActualQuantity
            AND unit_price = @UnitPrice AND vat_rate = @VatRate
            AND note = @Note AND amount = @UnitPrice * @ActualQuantity * @VatRate";

            // Check line 1 -> should insert
            int line1Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = command.Lines[0].ProductId,
                UnitId = command.Lines[0].UnitId,
                DocumentQuantity = command.Lines[0].DocumentQuantity,
                ActualQuantity = command.Lines[0].ActualQuantity,
                UnitPrice = command.Lines[0].UnitPrice,
                VatRate = command.Lines[0].VatRate,
                Note = command.Lines[0].Note
            });

            Assert.Equal(1, line1Count);

            // Check line 2 -> should insert
            int line2Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = command.Lines[1].ProductId,
                UnitId = command.Lines[1].UnitId,
                DocumentQuantity = command.Lines[1].DocumentQuantity,
                ActualQuantity = command.Lines[1].ActualQuantity,
                UnitPrice = command.Lines[1].UnitPrice,
                VatRate = command.Lines[1].VatRate,
                Note = command.Lines[1].Note
            });

            Assert.Equal(1, line2Count);

            // Check inventory_balances table -> DRAFT docs are not recorded in the balance
            string balanceLineSql = @"
            SELECT COUNT(1) FROM inventory_balances
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId
            AND unit_id = @UnitId
            ";

            // Check inventory_balances of line 1 -> should not recorded
            int balanceLine1Count = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = command.Lines[0].ProductId,
                UnitId = command.Lines[0].UnitId
            });

            Assert.Equal(0, balanceLine1Count);

            // Check inventory_balances of line 2 -> should not recorded
            int balanceLine2Count = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = command.Lines[1].ProductId,
                UnitId = command.Lines[1].UnitId
            });

            Assert.Equal(0, balanceLine2Count);
        }
        finally
        {
            if (inserted != null)
            {
                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
                ", new { DocumentId = inserted.DocumentId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipts WHERE document_id = @DocumentId
                ", new { DocumentId = inserted.DocumentId });

                await dataRand.DeleteDocument(inserted.DocumentId);
            }
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeletePeriod(periodId);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }

    [Fact]
    public async Task Create_Posted_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

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

            Lines = new List<GoodsReceiptLineInput>
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                },
                new GoodsReceiptLineInput
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

            // Check documents table -> should insert
            var actualDocId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT document_id FROM documents
            WHERE document_no = @DocumentNo 
            AND posting_date = @PostingDate
            AND document_date = @DocumentDate 
            AND period_id = @PeriodId
            AND document_type = @DocumentType AND created_by = @CreatedBy
            AND note = @Note AND deleted_by IS NULL AND deleted_at IS NULL
            AND deleted_reason IS NULL AND status = @Status
            "
            , new
            {
                DocumentNo = inserted.DocumentNo,
                PostingDate = command.PostingDate,
                DocumentDate = command.DocumentDate,
                PeriodId = periodId,
                DocumentType = DocumentType.NK.ToString(),
                CreatedBy = currentUser.UserId,
                Note = command.Note,
                Status = DocumentStatus.POSTED.ToString()
            });

            Assert.Equal(inserted.DocumentId, actualDocId);

            // Check goods_receipts -> should insert
            var actualReceiptId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT document_id
            FROM goods_receipts WHERE 
            warehouse_id = @WarehouseId AND shipper_name = @ShipperName
            AND document_id = @DocumentId
            "
            , new
            {
                WarehouseId = warehouseId,
                ShipperName = command.ShipperName,
                DocumentId = inserted.DocumentId
            });

            Assert.Equal(inserted.DocumentId, actualReceiptId);

            // Check goods_receipt_lines -> lines should be inserted
            string lineCountSql = @"SELECT COUNT(1)
            FROM goods_receipt_lines
            WHERE document_id = @DocumentId AND product_id = @ProductId
            AND unit_id = @UnitId AND document_quantity = @DocumentQuantity
            AND actual_quantity = @ActualQuantity
            AND unit_price = @UnitPrice AND vat_rate = @VatRate
            AND note = @Note AND amount = @UnitPrice * @ActualQuantity * @VatRate";

            // Check line 1 -> should inserted
            int line1Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = command.Lines[0].ProductId,
                UnitId = command.Lines[0].UnitId,
                DocumentQuantity = command.Lines[0].DocumentQuantity,
                ActualQuantity = command.Lines[0].ActualQuantity,
                UnitPrice = command.Lines[0].UnitPrice,
                VatRate = command.Lines[0].VatRate,
                Note = command.Lines[0].Note
            });

            Assert.Equal(1, line1Count);

            // Check line 2 -> should inserted
            int line2Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = command.Lines[1].ProductId,
                UnitId = command.Lines[1].UnitId,
                DocumentQuantity = command.Lines[1].DocumentQuantity,
                ActualQuantity = command.Lines[1].ActualQuantity,
                UnitPrice = command.Lines[1].UnitPrice,
                VatRate = command.Lines[1].VatRate,
                Note = command.Lines[1].Note
            });

            Assert.Equal(1, line2Count);

            // Check inventory_balances -> POSTED docs are recorded in the balance
            string balanceLineSql = @"
            SELECT COUNT(1) FROM inventory_balances
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId
            AND unit_id = @UnitId AND quantity = @Quantity AND amount = @Amount
            ";

            // Check balance of line 1 -> should be recorded
            int balanceLine1Count = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = command.Lines[0].ProductId,
                UnitId = command.Lines[0].UnitId,
                Quantity = command.Lines[0].ActualQuantity,
                Amount = command.Lines[0].ActualQuantity * command.Lines[0].UnitPrice * command.Lines[0].VatRate
            });

            Assert.Equal(1, balanceLine1Count);

            // Check balance of line 2 -> should be recorded
            int balanceLine2Count = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = command.Lines[1].ProductId,
                UnitId = command.Lines[1].UnitId,
                Quantity = command.Lines[1].ActualQuantity,
                Amount = command.Lines[1].ActualQuantity * command.Lines[1].UnitPrice * command.Lines[1].VatRate
            });

            Assert.Equal(1, balanceLine2Count);
        }
        finally
        {
            if (inserted != null)
            {
                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_balances WHERE warehouse_id = @WarehouseId
                ", new { WarehouseId = warehouseId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
                ", new { DocumentId = inserted.DocumentId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipts WHERE document_id = @DocumentId
                ", new { DocumentId = inserted.DocumentId });

                await dataRand.DeleteDocument(inserted.DocumentId);
            }
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeletePeriod(periodId);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }

    [Fact]
    public async Task Create_Posted_Should_Increase_Balances()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        int baseUnitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int periodId = await dataRand.RandomPeriod();
        int productId1 = await dataRand.RandomProduct(baseUnitId, supplierId);
        int productId2 = await dataRand.RandomProduct(baseUnitId, supplierId);
        await dataRand.InsertProductUnit(productId1, baseUnitId);
        await dataRand.InsertProductUnit(productId2, baseUnitId);
        int branchId = await dataRand.RandomBranch();
        int warehouseId = await dataRand.RandomWarehouse(branchId);

        var command1 = new CreateGoodsReceiptCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddDays(1),
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId,

            Lines = new List<GoodsReceiptLineInput>
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                },
                new GoodsReceiptLineInput
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

        var command2 = new CreateGoodsReceiptCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddDays(1),
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId,

            Lines = new List<GoodsReceiptLineInput>
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 20,
                    ActualQuantity = 20,
                    UnitPrice = 200_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                }
            }
        };

        CreateDocumentResponse? inserted1 = null, inserted2 = null;

        try
        {
            // First insert -> insert balances for product1 and product2
            inserted1 = await sender.Send(command1, CancellationToken.None);

            // Second insert -> should UPDATE (increase) balances for product1 and product2
            inserted2 = await sender.Send(command2, CancellationToken.None);

            // Check balances
            string balanceLineSql = @"
            SELECT COUNT(1) FROM inventory_balances
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId
            AND unit_id = @UnitId AND quantity = @Quantity AND amount = @Amount
            ";

            int product1BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = command1.Lines[0].ProductId,
                UnitId = command1.Lines[0].UnitId,
                Quantity = command1.Lines[0].ActualQuantity + command2.Lines[0].ActualQuantity,
                Amount = command1.Lines[0].ActualQuantity * command1.Lines[0].UnitPrice * command1.Lines[0].VatRate
                + command2.Lines[0].ActualQuantity * command2.Lines[0].UnitPrice * command2.Lines[0].VatRate
            });

            Assert.Equal(1, product1BalanceCount);

            int product2BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(balanceLineSql
            , new
            {
                WarehouseId = warehouseId,
                ProductId = command1.Lines[1].ProductId,
                UnitId = command1.Lines[1].UnitId,
                Quantity = command1.Lines[1].ActualQuantity,
                Amount = command1.Lines[1].ActualQuantity * command1.Lines[1].UnitPrice * command1.Lines[1].VatRate
            });

            Assert.Equal(1, product2BalanceCount);
        }
        finally
        {
            if (inserted1 != null)
            {
                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_balances WHERE warehouse_id = @WarehouseId
                ", new { WarehouseId = warehouseId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
                ", new { DocumentId = inserted1.DocumentId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipts WHERE document_id = @DocumentId
                ", new { DocumentId = inserted1.DocumentId });

                await dataRand.DeleteDocument(inserted1.DocumentId);
            }

            if (inserted2 != null)
            {
                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
                ", new { DocumentId = inserted2.DocumentId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipts WHERE document_id = @DocumentId
                ", new { DocumentId = inserted2.DocumentId });

                await dataRand.DeleteDocument(inserted2.DocumentId);
            }

            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeletePeriod(periodId);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }

    [Fact]
    public async Task Create_Should_Throw_Invalid_PostingDate()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

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
            PostingDate = DateTime.UtcNow.AddYears(1),
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId,

            Lines = new List<GoodsReceiptLineInput>
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                },
                new GoodsReceiptLineInput
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
            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
                inserted = await sender.Send(command, CancellationToken.None);
            });

            Assert.Equal("invalid_postingdate", ex.Code);
        }
        finally
        {
            if (inserted != null)
            {
                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_balances WHERE warehouse_id = @WarehouseId
                ", new { WarehouseId = warehouseId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
                ", new { DocumentId = inserted.DocumentId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipts WHERE document_id = @DocumentId
                ", new { DocumentId = inserted.DocumentId });

                await dataRand.DeleteDocument(inserted.DocumentId);
            }
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeletePeriod(periodId);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }

    [Fact]
    public async Task Create_Should_Throw_Period_Closed()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

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

            Lines = new List<GoodsReceiptLineInput>
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 10,
                    ActualQuantity = 10,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                },
                new GoodsReceiptLineInput
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
            await dbSession.Connection.ExecuteAsync(@"
            UPDATE periods SET is_closed = true WHERE period_id = @PeriodId
            ", new { PeriodId = periodId }); 

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
                inserted = await sender.Send(command, CancellationToken.None);
            });

            Assert.Equal("period_closed", ex.Code);
        }
        finally
        {
            if (inserted != null)
            {
                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM inventory_balances WHERE warehouse_id = @WarehouseId
                ", new { WarehouseId = warehouseId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
                ", new { DocumentId = inserted.DocumentId });

                await dbSession.Connection.ExecuteAsync(@"
                DELETE FROM goods_receipts WHERE document_id = @DocumentId
                ", new { DocumentId = inserted.DocumentId });

                await dataRand.DeleteDocument(inserted.DocumentId);
            }
            await dataRand.DeleteProductUnits(productId1);
            await dataRand.DeleteProductUnits(productId2);
            await dataRand.DeletePeriod(periodId);
            await dataRand.DeleteProduct(productId1);
            await dataRand.DeleteProduct(productId2);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
            await dataRand.DeleteUnit(baseUnitId);
        }
    }
}
