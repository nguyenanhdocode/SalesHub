// using Application.Exceptions;
// using Application.Features.GoodsIssues.Create;
// using Application.Features.GoodsReceipts.Create;
// using Application.Features.Products.Create;
// using Application.Interfaces.Security;
// using Application.Models.Documents;
// using Application.Shared;
// using Dapper;
// using FluentValidation;
// using MediatR;
// using Microsoft.Extensions.DependencyInjection;
// using Npgsql;

// namespace Application.IntegrationTests.GoodsReceipts;

// public class CreateGoodsIssueTests : IClassFixture<ApplicationFixture> , IAsyncLifetime
// {
//     private readonly ApplicationFixture _fixture;
//     private readonly IServiceScope _scope;

//     public CreateGoodsIssueTests(ApplicationFixture fixture)
//     {
//         _fixture = fixture;
//         _scope = _fixture.CreateScope();
//     }

//     [Fact]
//     public async Task Create_Draft_Should_Success()
//     {
//         var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
//         var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
//         var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
//         var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

//         int baseUnitId = await dataRand.RandomUnit();
//         int supplierId = await dataRand.RandomSupplier();
//         int periodId = await dataRand.RandomPeriod();
//         int productId1 = await dataRand.RandomProduct(baseUnitId, supplierId);
//         int productId2 = await dataRand.RandomProduct(baseUnitId, supplierId);
//         await dataRand.InsertProductUnit(productId1, baseUnitId);
//         await dataRand.InsertProductUnit(productId2, baseUnitId);
//         int branchId = await dataRand.RandomBranch();
//         int warehouseId = await dataRand.RandomWarehouse(branchId);

//         var command = new CreateGoodsIssueCommand
//         {
//             DocumentDate = DateTime.UtcNow,
//             PeriodId = periodId,
//             Note = "Note",
//             PostingDate = DateTime.UtcNow.AddDays(1),
//             Status = Shared.DocumentStatus.DRAFT,
//             WarehouseId = warehouseId,
//             Reason = "Reason",

//             Lines = new List<CreateGoodsIssueLineInput>
//             {
//                 new CreateGoodsIssueLineInput
//                 {
//                     ProductId = productId1,
//                     DocumentQuantity = 10,
//                     ActualQuantity = 10,
//                     UnitPrice = 100_000,
//                     Note = "Note1",
//                     UnitId = baseUnitId
//                 }
//             }
//         };

//         CreateDocumentResponse? inserted = null;

//         try
//         {
//             inserted = await sender.Send(command, CancellationToken.None);

//             // Check documents table -> should inserted
//             var actualDocId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
//             SELECT document_id FROM documents
//             WHERE document_no = @DocumentNo 
//             AND posting_date = @PostingDate
//             AND document_date = @DocumentDate 
//             AND period_id = @PeriodId
//             AND document_type = @DocumentType AND created_by = @CreatedBy
//             AND note = @Note AND deleted_by IS NULL AND deleted_at IS NULL
//             AND deleted_reason IS NULL AND status = @Status
//             "
//             , new
//             {
//                 DocumentNo = inserted.DocumentNo,
//                 PostingDate = command.PostingDate,
//                 DocumentDate = command.DocumentDate,
//                 PeriodId = periodId,
//                 DocumentType = DocumentType.XK.ToString(),
//                 CreatedBy = currentUser.UserId,
//                 Note = command.Note,
//                 Status = DocumentStatus.DRAFT.ToString()
//             });

//             Assert.Equal(inserted.DocumentId, actualDocId);

//             // Check goods_receipts table -> should inserted
//             var actualReceiptId = await dbSession.Connection.ExecuteScalarAsync<Guid>(@"
//             SELECT document_id
//             FROM goods_issues WHERE 
//             warehouse_id = @WarehouseId AND reason = @Reason
//             AND document_id = @DocumentId
//             "
//             , new
//             {
//                 WarehouseId = warehouseId,
//                 Reason = "Reason",
//                 DocumentId = inserted.DocumentId
//             });

//             Assert.Equal(inserted.DocumentId, actualReceiptId);

//             string lineCountSql = @"SELECT COUNT(1)
//             FROM goods_issue_lines
//             WHERE document_id = @DocumentId AND product_id = @ProductId
//             AND unit_id = @UnitId AND document_quantity = @DocumentQuantity
//             AND actual_quantity = @ActualQuantity
//             AND unit_price = @UnitPrice
//             AND note = @Note AND amount = @UnitPrice * @ActualQuantity";

//             // Check line 1 -> should insert
//             int line1Count = await dbSession.Connection.ExecuteScalarAsync<int>(lineCountSql, new
//             {
//                 DocumentId = inserted.DocumentId,
//                 ProductId = command.Lines[0].ProductId,
//                 UnitId = command.Lines[0].UnitId,
//                 DocumentQuantity = command.Lines[0].DocumentQuantity,
//                 ActualQuantity = command.Lines[0].ActualQuantity,
//                 UnitPrice = command.Lines[0].UnitPrice,
//                 Note = command.Lines[0].Note
//             });

//             Assert.Equal(1, line1Count);
//         }
//         finally
//         {
//             if (inserted != null)
//             {
//                 await dbSession.Connection.ExecuteAsync(@"
//                 DELETE FROM goods_issue_lines WHERE document_id = @DocumentId
//                 ", new { DocumentId = inserted.DocumentId });

//                 await dbSession.Connection.ExecuteAsync(@"
//                 DELETE FROM goods_issues WHERE document_id = @DocumentId
//                 ", new { DocumentId = inserted.DocumentId });

//                 await dataRand.DeleteDocument(inserted.DocumentId);
//             }
//             await dataRand.DeleteProductUnits(productId1);
//             await dataRand.DeleteProductUnits(productId2);
//             await dataRand.DeletePeriod(periodId);
//             await dataRand.DeleteProduct(productId1);
//             await dataRand.DeleteProduct(productId2);
//             await dataRand.DeleteSupplier(supplierId);
//             await dataRand.DeleteWarehouse(warehouseId);
//             await dataRand.DeleteBranch(branchId);
//             await dataRand.DeleteUnit(baseUnitId);
//         }
//     }

//     [Fact]
//     public async Task Create_Posted_Should_Throw_Insufficient_Inventory()
//     {
//         var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
//         var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
//         var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
//         var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

//         int baseUnitId = await dataRand.RandomUnit();
//         int supplierId = await dataRand.RandomSupplier();
//         int periodId = await dataRand.RandomPeriod();
//         int productId1 = await dataRand.RandomProduct(baseUnitId, supplierId);
//         int productId2 = await dataRand.RandomProduct(baseUnitId, supplierId);
//         await dataRand.InsertProductUnit(productId1, baseUnitId);
//         await dataRand.InsertProductUnit(productId2, baseUnitId);
//         int branchId = await dataRand.RandomBranch();
//         int warehouseId = await dataRand.RandomWarehouse(branchId);

//         var command = new CreateGoodsIssueCommand
//         {
//             DocumentDate = DateTime.UtcNow,
//             PeriodId = periodId,
//             Note = "Note",
//             PostingDate = DateTime.UtcNow.AddDays(1),
//             Status = Shared.DocumentStatus.POSTED,
//             WarehouseId = warehouseId,
//             Reason = "Reason",

//             Lines = new List<CreateGoodsIssueLineInput>
//             {
//                 new CreateGoodsIssueLineInput
//                 {
//                     ProductId = productId1,
//                     DocumentQuantity = 10,
//                     ActualQuantity = 10,
//                     UnitPrice = 100_000,
//                     Note = "Note1",
//                     UnitId = baseUnitId
//                 }
//             }
//         };

//         CreateDocumentResponse? inserted = null;

//         try
//         {

//             var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
//             {
//                 inserted = await sender.Send(command, CancellationToken.None);
//             });

//             Assert.Equal("insufficient_inventory", ex.Code);
//         }
//         finally
//         {
//             if (inserted != null)
//             {
//                 await dbSession.Connection.ExecuteAsync(@"
//                 DELETE FROM goods_issue_lines WHERE document_id = @DocumentId
//                 ", new { DocumentId = inserted.DocumentId });

//                 await dbSession.Connection.ExecuteAsync(@"
//                 DELETE FROM goods_issues WHERE document_id = @DocumentId
//                 ", new { DocumentId = inserted.DocumentId });

//                 await dataRand.DeleteDocument(inserted.DocumentId);
//             }
//             await dataRand.DeleteProductUnits(productId1);
//             await dataRand.DeleteProductUnits(productId2);
//             await dataRand.DeletePeriod(periodId);
//             await dataRand.DeleteProduct(productId1);
//             await dataRand.DeleteProduct(productId2);
//             await dataRand.DeleteSupplier(supplierId);
//             await dataRand.DeleteWarehouse(warehouseId);
//             await dataRand.DeleteBranch(branchId);
//             await dataRand.DeleteUnit(baseUnitId);
//         }
//     }

//     public async Task DisposeAsync()
//     {
//         _scope.Dispose();
//     }

//     public Task InitializeAsync()
//     {
//         return Task.CompletedTask;
//     }
// }
