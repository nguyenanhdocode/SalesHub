using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Warehouses.Create;
using Application.Features.Warehouses.List;
using Application.Shared;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Warehouses;

public class ListWarehouseTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly string _prefix = Guid.NewGuid().ToString("N")[..25];
    private readonly List<int> _warehouseIds = [];

    public ListWarehouseTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task DisposeAsync()
    {
        using var scope = _fixture.CreateScope();
        var dataSeed = scope.ServiceProvider.GetRequiredService<DataRandom>();

        foreach (int id in _warehouseIds)
        {
            await dataSeed.DeleteWarehouse(id);
        }
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataSeed = scope.ServiceProvider.GetRequiredService<DataRandom>();
        
        int branchId1 = 0, branchId2 = 0;

        branchId1 = await dataSeed.RandomBranch();
        branchId2 = await dataSeed.RandomBranch();

        var command1 = new CreateWarehouseCommand
        {
          Code = $"{_prefix}-kho-huy-govap",
          Name = "Kho hàng hủy gò vấp",
          BranchId = branchId1
        };

        var command2 = new CreateWarehouseCommand
        {
          Code = $"{_prefix}-kho-govap",
          Name = "Kho hàng gò vấp",
          BranchId = branchId1
        };

        var command3 = new CreateWarehouseCommand
        {
          Code = $"{_prefix}-kho-tb-govap",
          Name = "Kho hàng trưng bày gò vấp",
          BranchId = branchId1
        };

        var command4 = new CreateWarehouseCommand
        {
          Code = $"{_prefix}-kho-tb-thuduc",
          Name = "Kho hàng trưng bày Thủ Đức",
          BranchId = branchId2
        };

        var command5 = new CreateWarehouseCommand
        {
          Code = $"{_prefix}-kho-thuduc",
          Name = "Kho hàng Thủ Đức",
          BranchId = branchId2
        };

        int warehouseId1 = await sender.Send(command1, CancellationToken.None);
        int warehouseId2 = await sender.Send(command2, CancellationToken.None);
        int warehouseId3 = await sender.Send(command3, CancellationToken.None);
        int warehouseId4 = await sender.Send(command4, CancellationToken.None);
        int warehouseId5 = await sender.Send(command5, CancellationToken.None);

        Assert.NotEqual(0, warehouseId1);
        Assert.NotEqual(0, warehouseId2);
        Assert.NotEqual(0, warehouseId3);
        Assert.NotEqual(0, warehouseId4);
        Assert.NotEqual(0, warehouseId5);

        _warehouseIds.Add(warehouseId1);
        _warehouseIds.Add(warehouseId2);
        _warehouseIds.Add(warehouseId3);
        _warehouseIds.Add(warehouseId4);
        _warehouseIds.Add(warehouseId5);

        await dbSession.Connection.ExecuteAsync(@"
        UPDATE warehouses SET active = false
        WHERE warehouse_id = @WarehouseId
        ", new { WarehouseId = warehouseId1 });
    }

    [Fact]
    public async Task List_Should_Return_All()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery {}, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(_warehouseIds.Count, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery { Code = $"{_prefix}-kho-huy-govap" }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-kho-huy-govap");
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery { Code = $"govap" }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery { Code = $"{_prefix}-something" }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery { Name = "Kho hàng hủy gò vấp" }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-kho-huy-govap");
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery { Name = "thủ đức" }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery { Name = $"{_prefix}-thủ đưcc" }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_Active_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery { Active = false }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-kho-huy-govap");
    }

    [Fact]
    public async Task Filter_By_Active_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery { Active = true }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Should_Filter_By_All_Fields()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListWarehouseQuery
        {
            Code = $"{_prefix}-kho-huy-govap",
            Name = "Kho hàng hủy gò vấp",
            Active = false
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-kho-huy-govap");
    }

    [Fact]
    public async Task Paginate_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListWarehouseQuery { PageNum = 1, PageSize = 2, Code = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(2, count1);

        var res2 = await sender.Send(new ListWarehouseQuery { PageNum = 2, PageSize = 2, Code = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(2, count2);

        var res3 = await sender.Send(new ListWarehouseQuery { PageNum = 3, PageSize = 2, Code = _prefix}
        , CancellationToken.None);

        Assert.Single(res3.Rows, p => p.Code.StartsWith(_prefix));
    }

    [Fact]
    public async Task Paginate_With_Wrong_Number_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListWarehouseQuery { PageNum = 0, PageSize = 50, Code = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res1.PageNumer);
        Assert.Equal(50, res1.PageSize);
        Assert.Equal(5, count1);

        var res2 = await sender.Send(new ListWarehouseQuery { PageNum = 1, PageSize = 0, Code = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res2.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res2.PageSize);
        Assert.Equal(5, count2);

        var res3 = await sender.Send(new ListWarehouseQuery { PageNum = 0, PageSize = 0, Code = _prefix}
        , CancellationToken.None);
        int count3 = res3.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res3.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res3.PageSize);
        Assert.Equal(5, count3);
    }
}