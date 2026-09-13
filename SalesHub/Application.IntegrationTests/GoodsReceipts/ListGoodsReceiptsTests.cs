using Application.Exceptions;
using Application.Features.GoodsReceipts.Create;
using Application.Features.GoodsReceipts.List;
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

public class ListGoodsReceiptsTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;
    private readonly List<CreateDocumentResponse> _documents = new();

    int baseUnitId = 0;
    int supplierId = 0;
    int productId1 = 0, productId2 = 0;
    int branchId1 = 0, branchId2 = 0;
    int warehouseId1 = 0, warehouseId2 = 0;
    int periodId1 = 0, periodId2 = 0;
    CreateDocumentResponse? _command1Res, _command2Res, _command3Res, _command4Res;
    string _prefix = Guid.NewGuid().ToString();

    public ListGoodsReceiptsTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    [Fact]
    public async Task List_Should_Return_All()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListGoodsReceiptsQuery {}, CancellationToken.None);
        int count = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix)).Count();
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_DocumentNo_Should_Return_One()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            DocumentNo = _command1Res?.DocumentNo
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Single(rows, p => p.DocumentNo == _command1Res?.DocumentNo);
    }

    [Fact]
    public async Task Filter_By_DocumentNo_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            DocumentNo = _prefix
        }, CancellationToken.None);

        int count = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix)).Count();
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_DocumentNo_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            DocumentNo = "NONE"
        }, CancellationToken.None);

        int count = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix)).Count();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Filter_By_PeriodIds_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            FilterByPeriod = true,
            PeriodIds = [periodId1]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Equal(2, rows.Count());

        Assert.Single(rows, p => p.DocumentId == _command1Res?.DocumentId);
        Assert.Single(rows, p => p.DocumentId == _command3Res?.DocumentId);
    }

    [Fact]
    public async Task Filter_By_PeriodIds_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            FilterByPeriod = true,
            PeriodIds = [int.MaxValue]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Filter_By_Date_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            FilterByPeriod = false,
            FromDate = new DateTime(2026, 1, 1),
            ToDate = new DateTime(2026, 4, 1),
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Equal(3, rows.Count());

        Assert.Single(rows, p => p.DocumentId == _command1Res?.DocumentId);
        Assert.Single(rows, p => p.DocumentId == _command2Res?.DocumentId);
        Assert.Single(rows, p => p.DocumentId == _command3Res?.DocumentId);
    }

    [Fact]
    public async Task Filter_By_Date_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            FilterByPeriod = false,
            FromDate = new DateTime(2025, 1, 1),
            ToDate = new DateTime(2025, 4, 1),
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Filter_By_CreatedBy_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            CreatedBy = "integration"
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Equal(4, rows.Count());
    }

    [Fact]
    public async Task Filter_By_CreatedBy_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            CreatedBy = "NONE"
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Filter_By_ShipperName_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            ShipperName = "Nguyễn Văn"
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Equal(2, rows.Count());

        Assert.Single(rows, p => p.DocumentId == _command1Res?.DocumentId);
        Assert.Single(rows, p => p.DocumentId == _command2Res?.DocumentId);
    }

    [Fact]
    public async Task Filter_By_ShipperName_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            ShipperName = "NONE"
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Filter_By_WarehouseIds_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            WarehouseIds = [warehouseId2]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Equal(2, rows.Count());

        Assert.Single(rows, p => p.DocumentId == _command3Res?.DocumentId);
        Assert.Single(rows, p => p.DocumentId == _command4Res?.DocumentId);
    }

    [Fact]
    public async Task Filter_By_WarehouseIds_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            WarehouseIds = [int.MaxValue]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Filter_By_BranchIds_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            BranchIds = [branchId2]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Equal(2, rows.Count());

        Assert.Single(rows, p => p.DocumentId == _command3Res?.DocumentId);
        Assert.Single(rows, p => p.DocumentId == _command4Res?.DocumentId);
    }

    [Fact]
    public async Task Filter_By_BranchIds_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListGoodsReceiptsQuery
        {
            BranchIds = [int.MaxValue]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Paginate_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListGoodsReceiptsQuery { PageNum = 1, PageSize = 3, DocumentNo = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Count();

        Assert.Equal(3, count1);

        var res2 = await sender.Send(new ListGoodsReceiptsQuery { PageNum = 2, PageSize = 3, DocumentNo = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Count();

        Assert.Equal(1, count2);
    }

    [Fact]
    public async Task Paginate_With_Wrong_Number_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListGoodsReceiptsQuery { PageNum = 0, PageSize = 50, DocumentNo = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Count();

        Assert.Equal(1, res1.PageNumer);
        Assert.Equal(50, res1.PageSize);
        Assert.Equal(4, count1);

        var res2 = await sender.Send(new ListGoodsReceiptsQuery { PageNum = 1, PageSize = 0, DocumentNo = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Count();

        Assert.Equal(1, res2.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res2.PageSize);
        Assert.Equal(4, count2);

        var res3 = await sender.Send(new ListGoodsReceiptsQuery { PageNum = 0, PageSize = 0, DocumentNo = _prefix}
        , CancellationToken.None);
        int count3 = res3.Rows.Count();

        Assert.Equal(1, res3.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res3.PageSize);
        Assert.Equal(4, count3);
    }

    public async Task InitializeAsync()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        baseUnitId = await dataRand.RandomUnit();
        supplierId = await dataRand.RandomSupplier();
        productId1 = await dataRand.RandomProduct(baseUnitId, supplierId);
        productId2 = await dataRand.RandomProduct(baseUnitId, supplierId);
        await dataRand.InsertProductUnit(productId1, baseUnitId);
        await dataRand.InsertProductUnit(productId2, baseUnitId);
        branchId1 = await dataRand.RandomBranch();
        branchId2 = await dataRand.RandomBranch();
        warehouseId1 = await dataRand.RandomWarehouse(branchId1);
        warehouseId2 = await dataRand.RandomWarehouse(branchId2);

        var period1Code = Guid.NewGuid().ToString();
        periodId1 = await dbSession.Connection.ExecuteScalarAsync<int>(@"
        INSERT INTO periods (code, name, from_date, to_date)
        VALUES (@Code, @Name, @FromDate, @ToDate)
        RETURNING period_id;
        "
        , new
        {
            Code = period1Code,
            Name = $"{period1Code}name",
            FromDate = new DateTime(2026, 01, 01),
            ToDate = new DateTime(2026, 03, 01)

        });

        var period2Code = Guid.NewGuid().ToString();
        periodId2 = await dbSession.Connection.ExecuteScalarAsync<int>(@"
        INSERT INTO periods (code, name, from_date, to_date)
        VALUES (@Code, @Name, @FromDate, @ToDate)
        RETURNING period_id;
        "
        , new
        {
            Code = period2Code,
            Name = $"{period2Code}name",
            FromDate = new DateTime(2026, 04, 01),
            ToDate = new DateTime(2026, 06, 30)

        });

        var command1 = new CreateGoodsReceiptCommand
        {
            DocumentNo = $"{_prefix}-1",
            DocumentDate = DateTime.UtcNow,
            PeriodId = periodId1,
            Note = $"{_prefix} Note 1",
            PostingDate = new DateTime(2026, 01, 01),
            ShipperName = "Nguyễn Văn A",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId1,

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
                }
            }
        };

        var command2 = new CreateGoodsReceiptCommand
        {
            DocumentNo = $"{_prefix}-2",
            DocumentDate = DateTime.UtcNow.AddDays(1),
            PeriodId = periodId2,
            Note = $"{_prefix} Note 2",
            PostingDate = new DateTime(2026, 04, 01),
            ShipperName = "Nguyễn Văn B",
            Status = Shared.DocumentStatus.DRAFT,
            WarehouseId = warehouseId1,

            Lines = new List<GoodsReceiptLineInput>
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId2,
                    DocumentQuantity = 15,
                    ActualQuantity = 15,
                    UnitPrice = 120_000,
                    Note = "Note2",
                    VatRate = 1.2m,
                    UnitId = baseUnitId
                }
            }
        };

        var command3 = new CreateGoodsReceiptCommand
        {
            DocumentNo = $"{_prefix}-3",
            DocumentDate = DateTime.UtcNow.AddDays(2),
            PeriodId = periodId1,
            Note = $"{_prefix} Note 3",
            PostingDate = new DateTime(2026, 01, 10),
            ShipperName = "Trần Thị C",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId2,

            Lines = new List<GoodsReceiptLineInput>
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId1,
                    DocumentQuantity = 12,
                    ActualQuantity = 12,
                    UnitPrice = 110_000,
                    Note = "Note3",
                    VatRate = 1.1m,
                    UnitId = baseUnitId
                }
            }
        };

        var command4 = new CreateGoodsReceiptCommand
        {
            DocumentNo = $"{_prefix}-4",
            DocumentDate = DateTime.UtcNow.AddDays(3),
            PeriodId = periodId2,
            Note = $"{_prefix} Note 4",
            PostingDate = new DateTime(2026, 04, 20),
            ShipperName = "Lê Văn D",
            Status = Shared.DocumentStatus.POSTED,
            WarehouseId = warehouseId2,

            Lines = new List<GoodsReceiptLineInput>
            {
                new GoodsReceiptLineInput
                {
                    ProductId = productId2,
                    DocumentQuantity = 18,
                    ActualQuantity = 18,
                    UnitPrice = 130_000,
                    Note = "Note4",
                    VatRate = 1.3m,
                    UnitId = baseUnitId
                }
            }
        };

        _command1Res = await sender.Send(command1, CancellationToken.None);
        _command2Res = await sender.Send(command2, CancellationToken.None);
        _command3Res = await sender.Send(command3, CancellationToken.None);
        _command4Res = await sender.Send(command4, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, _command1Res.DocumentId);
        Assert.NotEqual(Guid.Empty, _command2Res.DocumentId);
        Assert.NotEqual(Guid.Empty, _command3Res.DocumentId);
        Assert.NotEqual(Guid.Empty, _command4Res.DocumentId);

        _documents.Add(_command1Res);
        _documents.Add(_command2Res);
        _documents.Add(_command3Res);
        _documents.Add(_command4Res);
    }

    public async Task DisposeAsync()
    {
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();

        foreach (var doc in _documents)
        {
            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipt_lines WHERE document_id = @DocumentId
            ", new { DocumentId = doc.DocumentId });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM goods_receipts WHERE document_id = @DocumentId
            ", new { DocumentId = doc.DocumentId });

            await dataRand.DeleteDocument(doc.DocumentId);
        }

        await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_balances WHERE warehouse_id = @WarehouseId
            ", new { WarehouseId = warehouseId1 });

        await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_balances WHERE warehouse_id = @WarehouseId
            ", new { WarehouseId = warehouseId2 });

        await dataRand.DeletePeriod(periodId1);
        await dataRand.DeletePeriod(periodId2);

        await dataRand.DeleteWarehouse(warehouseId1);
        await dataRand.DeleteWarehouse(warehouseId2);

        await dataRand.DeleteBranch(branchId1);
        await dataRand.DeleteBranch(branchId2);

        await dataRand.DeleteProductUnits(productId1);
        await dataRand.DeleteProductUnits(productId2);

        await dataRand.DeleteProduct(productId1);
        await dataRand.DeleteProduct(productId2);

        await dataRand.DeleteSupplier(supplierId);

        await dataRand.DeleteUnit(baseUnitId);

        _scope.Dispose();
    }
}
