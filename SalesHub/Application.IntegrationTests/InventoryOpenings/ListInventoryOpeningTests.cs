using Application.Exceptions;
using Application.Features.GoodsIssues.Create;
using Application.Features.GoodsReceipts.Create;
using Application.Features.InventoryOpenings.Create;
using Application.Features.InventoryOpenings.List;
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

public class ListInventoryOpeningTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
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

    public ListInventoryOpeningTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    [Fact]
    public async Task List_Should_Return_All()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListInventoryOpeningQuery { PageSize = int.MaxValue }, CancellationToken.None);
        int count = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix)).Count();
        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_DocumentNo_Should_Return_One()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListInventoryOpeningQuery
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

        var res = await sender.Send(new ListInventoryOpeningQuery
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

        var res = await sender.Send(new ListInventoryOpeningQuery
        {
            DocumentNo = "NONE"
        }, CancellationToken.None);

        int count = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix)).Count();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Filter_By_BranchIds_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListInventoryOpeningQuery
        {
            BranchIds = [branchId2]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Equal(2, rows.Count());

        Assert.Single(rows, p => p.DocumentId == _command2Res?.DocumentId);
        Assert.Single(rows, p => p.DocumentId == _command4Res?.DocumentId);
    }

    [Fact]
    public async Task Filter_By_BranchIds_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListInventoryOpeningQuery
        {
            BranchIds = [int.MaxValue]
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

        var res = await sender.Send(new ListInventoryOpeningQuery
        {
            WarehouseIds = [warehouseId2]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Equal(2, rows.Count());

        Assert.Single(rows, p => p.DocumentId == _command2Res?.DocumentId);
        Assert.Single(rows, p => p.DocumentId == _command4Res?.DocumentId);
    }

    [Fact]
    public async Task Filter_By_WarehouseIds_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var currentUser = _scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        var res = await sender.Send(new ListInventoryOpeningQuery
        {
            WarehouseIds = [int.MaxValue]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Filter_By_PeriodIds_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListInventoryOpeningQuery
        {
            PeriodIds = [periodId1]
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Equal(2, rows.Count());

        Assert.Single(rows, p => p.DocumentId == _command1Res?.DocumentId);
        Assert.Single(rows, p => p.DocumentId == _command2Res?.DocumentId);
    }

    [Fact]
    public async Task Filter_By_PeriodIds_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListInventoryOpeningQuery
        {
            PeriodIds = [int.MaxValue]
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

        var res = await sender.Send(new ListInventoryOpeningQuery
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

        var res = await sender.Send(new ListInventoryOpeningQuery
        {
            CreatedBy = "NONE"
        }, CancellationToken.None);

        var rows = res.Rows.Where(p => p.DocumentNo.StartsWith(_prefix));
        Assert.Empty(rows);
    }

    [Fact]
    public async Task Paginate_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListInventoryOpeningQuery { PageNum = 1, PageSize = 3, DocumentNo = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Count();

        Assert.Equal(3, count1);

        var res2 = await sender.Send(new ListInventoryOpeningQuery { PageNum = 2, PageSize = 3, DocumentNo = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Count();

        Assert.Equal(1, count2);
    }

    [Fact]
    public async Task Paginate_With_Wrong_Number_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListInventoryOpeningQuery { PageNum = 0, PageSize = 50, DocumentNo = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Count();

        Assert.Equal(1, res1.PageNumer);
        Assert.Equal(50, res1.PageSize);
        Assert.Equal(4, count1);

        var res2 = await sender.Send(new ListInventoryOpeningQuery { PageNum = 1, PageSize = 0, DocumentNo = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Count();

        Assert.Equal(1, res2.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res2.PageSize);
        Assert.Equal(4, count2);

        var res3 = await sender.Send(new ListInventoryOpeningQuery { PageNum = 0, PageSize = 0, DocumentNo = _prefix}
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

        var command1 = new CreateInventoryOpeningCommand
        {
            DocumentNo = $"{_prefix}-1",
            PeriodId = periodId1,
            Note = $"{_prefix} Note 1",
            WarehouseId = warehouseId1,

            Lines = new List<CreateInventoryOpeningLineInput>
            {
                new CreateInventoryOpeningLineInput
                {
                    ProductId = productId1,
                    Quantity = 10,
                    Amount = 100_000,
                    UnitId = baseUnitId
                }
            }
        };

        var command2 = new CreateInventoryOpeningCommand
        {
            DocumentNo = $"{_prefix}-2",
            PeriodId = periodId1,
            Note = $"{_prefix} Note 2",
            WarehouseId = warehouseId2,

            Lines = new List<CreateInventoryOpeningLineInput>
            {
                new CreateInventoryOpeningLineInput
                {
                    ProductId = productId2,
                    Quantity = 10,
                    Amount = 100_000,
                    UnitId = baseUnitId
                }
            }
        };

        var command3 = new CreateInventoryOpeningCommand
        {
            DocumentNo = $"{_prefix}-3",
            PeriodId = periodId2,
            Note = $"{_prefix} Note 3",
            WarehouseId = warehouseId1,

            Lines = new List<CreateInventoryOpeningLineInput>
            {
                new CreateInventoryOpeningLineInput
                {
                    ProductId = productId2,
                    Quantity = 10,
                    Amount = 100_000,
                    UnitId = baseUnitId
                }
            }
        };

        var command4 = new CreateInventoryOpeningCommand
        {
            DocumentNo = $"{_prefix}-4",
            PeriodId = periodId2,
            Note = $"{_prefix} Note 4",
            WarehouseId = warehouseId2,

            Lines = new List<CreateInventoryOpeningLineInput>
            {
                new CreateInventoryOpeningLineInput
                {
                    ProductId = productId1,
                    Quantity = 10,
                    Amount = 100_000,
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
            DELETE FROM inventory_opening_lines WHERE document_id = @DocumentId
            ", new { DocumentId = doc.DocumentId });

            await dbSession.Connection.ExecuteAsync(@"
            DELETE FROM inventory_openings WHERE document_id = @DocumentId
            ", new { DocumentId = doc.DocumentId });
        }

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
