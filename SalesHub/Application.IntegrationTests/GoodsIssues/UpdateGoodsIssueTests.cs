using Application.Exceptions;
using Application.Features.GoodsIssues.Create;
using Application.Features.GoodsIssues.Update;
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

public class UpdateGoodsIssueTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public UpdateGoodsIssueTests(ApplicationFixture fixture)
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

    public static TheoryData<UpdateGoodsIssueCommand, string> InvalidCommands => new()
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
            CreateInvalidCommand(command => command.Reason = new string('S', 1001)),
            "Reason"
        },
        {
            CreateInvalidCommand(command => command.Lines = []),
            "Lines"
        },
        {
            CreateInvalidCommand(command => command.Lines =
            [
                new UpdateGoodsIssueLineInput
                {
                    ProductId = 1,
                    UnitId = 1,
                    DocumentQuantity = 1,
                    ActualQuantity = 1,
                    UnitPrice = 1
                },
                new UpdateGoodsIssueLineInput
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

    private static UpdateGoodsIssueCommand CreateInvalidCommand(Action<UpdateGoodsIssueCommand> configure)
    {
        var command = new UpdateGoodsIssueCommand
        {
            PostingDate = DateTime.UtcNow,
            DocumentDate = DateTime.UtcNow,
            Note = "Note",
            Reason = "Reason",
            Status = DocumentStatus.DRAFT,
            Lines =
            [
                new UpdateGoodsIssueLineInput
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
    public async Task Update_Should_Throw_Validation_Exception(UpdateGoodsIssueCommand command, string expectedProperty)
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await sender.Send(command, CancellationToken.None);
        });

        Assert.Contains(exception.Errors, x => x.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task Update_Draft_Should_Success()
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
            Status = Shared.DocumentStatus.DRAFT,
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
            inserted = await sender.Send(command, CancellationToken.None);

            var updateCommand = new UpdateGoodsIssueCommand
            {
                DocumentId = inserted.DocumentId,
                DocumentDate = DateTime.UtcNow.AddDays(1),
                Note = "Note updated",
                PostingDate = DateTime.UtcNow.AddDays(2),
                Reason = "Lý do updated",
                Status = Shared.DocumentStatus.DRAFT,

                Lines = new List<UpdateGoodsIssueLineInput>
                {
                    new UpdateGoodsIssueLineInput
                    {
                        ProductId = productId1,
                        DocumentQuantity = 11,
                        ActualQuantity = 11,
                        UnitPrice = 100_000,
                        Note = "Note1 updated",
                        UnitId = baseUnitId
                    },
                    new UpdateGoodsIssueLineInput
                    {
                        ProductId = productId2,
                        DocumentQuantity = 20,
                        ActualQuantity = 20,
                        UnitPrice = 120_000,
                        Note = "Note2 updated",
                        UnitId = baseUnitId
                    }
                }
            };

            await sender.Send(updateCommand, CancellationToken.None);

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
                PostingDate = updateCommand.PostingDate,
                DocumentDate = updateCommand.DocumentDate,
                PeriodId = periodId,
                DocumentType = DocumentType.XK.ToString(),
                CreatedBy = currentUser.UserId,
                Note = updateCommand.Note,
                Status = DocumentStatus.DRAFT.ToString()
            });

            Assert.Equal(inserted.DocumentId, actualDocId);

            // Check goods_issues table -> should inserted
            var actualReceiptId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT document_id
            FROM goods_issues WHERE 
            warehouse_id = @WarehouseId AND reason = @Reason
            AND document_id = @DocumentId
            "
            , new
            {
                WarehouseId = warehouseId,
                Reason = updateCommand.Reason,
                DocumentId = inserted.DocumentId
            });

            Assert.Equal(inserted.DocumentId, actualReceiptId);

            string lineCountSql = @"SELECT COUNT(1)
            FROM goods_issue_lines
            WHERE document_id = @DocumentId AND product_id = @ProductId
            AND unit_id = @UnitId AND document_quantity = @DocumentQuantity
            AND actual_quantity = @ActualQuantity
            AND unit_price = @UnitPrice
            AND note = @Note AND amount = @UnitPrice * @ActualQuantity";

            // Check line 1 -> should insert
            int line1Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                DocumentQuantity = updateCommand.Lines[0].DocumentQuantity,
                ActualQuantity = updateCommand.Lines[0].ActualQuantity,
                UnitPrice = updateCommand.Lines[0].UnitPrice,
                Note = updateCommand.Lines[0].Note
            });

            Assert.Equal(1, line1Count);

            // Check line 2 -> should insert
            int line2Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                DocumentQuantity = updateCommand.Lines[1].DocumentQuantity,
                ActualQuantity = updateCommand.Lines[1].ActualQuantity,
                UnitPrice = updateCommand.Lines[1].UnitPrice,
                Note = updateCommand.Lines[1].Note
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
            DELETE FROM goods_issue_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_issues WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dataRand.DeleteDocument(inserted?.DocumentId ?? default);

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
    public async Task Update_Draft_To_Posted_Should_Success()
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
            Status = Shared.DocumentStatus.DRAFT,
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
            inserted = await sender.Send(command, CancellationToken.None);

            string insertBalanceSql = @"
            INSERT INTO inventory_balances (warehouse_id, product_id, unit_id, quantity, amount)
            VALUES (@WarehouseId, @ProductId, @UnitId, @Quantity, @Amount);
            ";

            var updateCommand = new UpdateGoodsIssueCommand
            {
                DocumentId = inserted.DocumentId,
                DocumentDate = DateTime.UtcNow.AddDays(1),
                Note = "Note updated",
                PostingDate = DateTime.UtcNow.AddDays(2),
                Reason = "Lý do updated",
                Status = Shared.DocumentStatus.POSTED,

                Lines = new List<UpdateGoodsIssueLineInput>
                {
                    new UpdateGoodsIssueLineInput
                    {
                        ProductId = productId1,
                        DocumentQuantity = 11,
                        ActualQuantity = 11,
                        UnitPrice = 100_000,
                        Note = "Note1 updated",
                        UnitId = baseUnitId
                    },
                    new UpdateGoodsIssueLineInput
                    {
                        ProductId = productId2,
                        DocumentQuantity = 20,
                        ActualQuantity = 20,
                        UnitPrice = 120_000,
                        Note = "Note2 updated",
                        UnitId = baseUnitId
                    }
                }
            };

            await dbSession.Connection.ExecuteAsync(insertBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                Quantity = updateCommand.Lines[0].ActualQuantity,
                Amount = updateCommand.Lines[0].ActualQuantity * updateCommand.Lines[0].UnitPrice,
            });

            await dbSession.Connection.ExecuteAsync(insertBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                Quantity = updateCommand.Lines[1].ActualQuantity,
                Amount = updateCommand.Lines[1].ActualQuantity * updateCommand.Lines[1].UnitPrice,
            });

            await sender.Send(updateCommand, CancellationToken.None);

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
                PostingDate = updateCommand.PostingDate,
                DocumentDate = updateCommand.DocumentDate,
                PeriodId = periodId,
                DocumentType = DocumentType.XK.ToString(),
                CreatedBy = currentUser.UserId,
                Note = updateCommand.Note,
                Status = DocumentStatus.POSTED.ToString()
            });

            Assert.Equal(inserted.DocumentId, actualDocId);

            // Check goods_issues table -> should inserted
            var actualReceiptId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT document_id
            FROM goods_issues WHERE 
            warehouse_id = @WarehouseId AND reason = @Reason
            AND document_id = @DocumentId
            "
            , new
            {
                WarehouseId = warehouseId,
                Reason = updateCommand.Reason,
                DocumentId = inserted.DocumentId
            });

            Assert.Equal(inserted.DocumentId, actualReceiptId);

            string lineCountSql = @"SELECT COUNT(1)
            FROM goods_issue_lines
            WHERE document_id = @DocumentId AND product_id = @ProductId
            AND unit_id = @UnitId AND document_quantity = @DocumentQuantity
            AND actual_quantity = @ActualQuantity
            AND unit_price = @UnitPrice
            AND note = @Note AND amount = @UnitPrice * @ActualQuantity";

            // Check line 1 -> should insert
            int line1Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                DocumentQuantity = updateCommand.Lines[0].DocumentQuantity,
                ActualQuantity = updateCommand.Lines[0].ActualQuantity,
                UnitPrice = updateCommand.Lines[0].UnitPrice,
                Note = updateCommand.Lines[0].Note
            });

            Assert.Equal(1, line1Count);

            // Check line 2 -> should insert
            int line2Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                DocumentQuantity = updateCommand.Lines[1].DocumentQuantity,
                ActualQuantity = updateCommand.Lines[1].ActualQuantity,
                UnitPrice = updateCommand.Lines[1].UnitPrice,
                Note = updateCommand.Lines[1].Note
            });

            Assert.Equal(1, line2Count);

            // Test balances
            string testBalanceSql = @"
            SELECT COUNT(1) FROM inventory_balances
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId AND unit_id = @UnitId
            AND quantity = @Quantity;
            ";

            int line1BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(testBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                Quantity = 0,
                // Amount = 0,
            });

            Assert.Equal(1, line1BalanceCount);

            int line2BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(testBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                Quantity = 0,
                // Amount = 0,
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

            await dataRand.DeleteDocument(inserted?.DocumentId ?? default);

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

            var updateCommand = new UpdateGoodsIssueCommand
            {
                DocumentId = inserted.DocumentId,
                DocumentDate = DateTime.UtcNow,
                Note = "Note",
                PostingDate = DateTime.UtcNow.AddDays(1),
                Reason = "Lý do",
                Status = Shared.DocumentStatus.POSTED,

                Lines = new List<UpdateGoodsIssueLineInput>
            {
                new UpdateGoodsIssueLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 5,
                    ActualQuantity = 5,
                    UnitPrice = 100_000,
                    Note = "Note1",
                    UnitId = baseUnitId
                },
                new UpdateGoodsIssueLineInput
                {
                    ProductId = productId3,
                    DocumentQuantity = 20,
                    ActualQuantity = 20,
                    UnitPrice = 120_000,
                    Note = "Note2",
                    UnitId = baseUnitId
                }
            }
            };

            await dbSession.Connection.ExecuteAsync(insertBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                Quantity = updateCommand.Lines[1].ActualQuantity,
                Amount = updateCommand.Lines[1].ActualQuantity * updateCommand.Lines[1].UnitPrice,
            });

            await sender.Send(updateCommand, CancellationToken.None);

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
                PostingDate = updateCommand.PostingDate,
                DocumentDate = updateCommand.DocumentDate,
                PeriodId = periodId,
                DocumentType = DocumentType.XK.ToString(),
                CreatedBy = currentUser.UserId,
                Note = updateCommand.Note,
                Status = DocumentStatus.POSTED.ToString()
            });

            Assert.Equal(inserted.DocumentId, actualDocId);

            // Check goods_issues table -> should inserted
            var actualReceiptId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
            SELECT document_id
            FROM goods_issues WHERE 
            warehouse_id = @WarehouseId AND reason = @Reason
            AND document_id = @DocumentId
            "
            , new
            {
                WarehouseId = warehouseId,
                Reason = updateCommand.Reason,
                DocumentId = inserted.DocumentId
            });

            Assert.Equal(inserted.DocumentId, actualReceiptId);

            string lineCountSql = @"SELECT COUNT(1)
            FROM goods_issue_lines
            WHERE document_id = @DocumentId AND product_id = @ProductId
            AND unit_id = @UnitId AND document_quantity = @DocumentQuantity
            AND actual_quantity = @ActualQuantity
            AND unit_price = @UnitPrice
            AND note = @Note AND amount = @UnitPrice * @ActualQuantity";

            // Check product 1 -> should update
            int line1Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                DocumentQuantity = updateCommand.Lines[0].DocumentQuantity,
                ActualQuantity = updateCommand.Lines[0].ActualQuantity,
                UnitPrice = updateCommand.Lines[0].UnitPrice,
                Note = updateCommand.Lines[0].Note
            });

            Assert.Equal(1, line1Count);

            // Check product 2 -> should insert
            int line2Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
            {
                DocumentId = inserted.DocumentId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                DocumentQuantity = updateCommand.Lines[1].DocumentQuantity,
                ActualQuantity = updateCommand.Lines[1].ActualQuantity,
                UnitPrice = updateCommand.Lines[1].UnitPrice,
                Note = updateCommand.Lines[1].Note
            });

            Assert.Equal(1, line2Count);

            int linesCount = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM goods_issue_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted.DocumentId });

            Assert.Equal(2, linesCount);

            // Test balances
            string testBalanceSql = @"
            SELECT COUNT(1) FROM inventory_balances
            WHERE warehouse_id = @WarehouseId AND product_id = @ProductId AND unit_id = @UnitId
            AND quantity = @Quantity;
            ";

            // Product1 balance
            int line1BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(testBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[0].ProductId,
                UnitId = updateCommand.Lines[0].UnitId,
                Quantity = updateCommand.Lines[0].ActualQuantity,
                // Amount = updateCommand.Lines[0].ActualQuantity * updateCommand.Lines[0].UnitPrice,
            });

            Assert.Equal(1, line1BalanceCount);

            // Product3 balance
            int line2BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(testBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = updateCommand.Lines[1].ProductId,
                UnitId = updateCommand.Lines[1].UnitId,
                Quantity = 0,
                // Amount = 0,
            });

            Assert.Equal(1, line2BalanceCount);

            // Product2 balance
            int product2BalanceCount = await dbSession.Connection.ExecuteScalarAsync<int>(testBalanceSql, new
            {
                WarehouseId = warehouseId,
                ProductId = command.Lines[1].ProductId,
                UnitId = command.Lines[1].UnitId,
                Quantity = command.Lines[1].ActualQuantity,
                Amount = command.Lines[1].ActualQuantity * command.Lines[1].UnitPrice,
            });

            Assert.Equal(1, product2BalanceCount);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_issue_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_issues WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dataRand.DeleteDocument(inserted?.DocumentId ?? default);

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

        var command = new CreateGoodsIssueCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddYears(1),
            Reason = "Lý do",
            Status = Shared.DocumentStatus.DRAFT,
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
            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
                inserted = await sender.Send(command, CancellationToken.None);
            });

            Assert.Equal("invalid_postingdate", ex.Code);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_issue_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_issues WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dataRand.DeleteDocument(inserted?.DocumentId ?? default);

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

        var command = new CreateGoodsIssueCommand
        {
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId,
            Note = "Note",
            PostingDate = DateTime.UtcNow.AddDays(1),
            Reason = "Lý do",
            Status = Shared.DocumentStatus.DRAFT,
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
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_issue_lines WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_issues WHERE document_id = @DocumentId
            ", new { DocumentId = inserted?.DocumentId ?? default });

            await dataRand.DeleteDocument(inserted?.DocumentId ?? default);

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
