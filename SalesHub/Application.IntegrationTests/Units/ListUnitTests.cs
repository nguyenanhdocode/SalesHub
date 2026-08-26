using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Units.Delete;
using Application.Features.Units.Get;
using Application.Features.Units.List;
using Application.Shared;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Units;

public class ListUnitTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly List<int> _unitIds = [];
    private string _prefix = Guid.NewGuid().ToString("N")[..25];

    public ListUnitTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task DisposeAsync()
    {
        using var scope = _fixture.CreateScope();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();

        foreach (var unitId in _unitIds)
        {
            await dataRand.DeleteUnit(unitId);
        }
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var command1 = new CreateUnitCommand
        {
            Code = $"{_prefix}-kg",
            Name = "Kilogram"
        };

        var command2 = new CreateUnitCommand
        {
            Code = $"{_prefix}-gr",
            Name = "Gram"
        };

        var command3 = new CreateUnitCommand
        {
            Code = $"{_prefix}-pcs",
            Name = "Pcs (cái)"
        };

        var command4 = new CreateUnitCommand
        {
            Code = $"{_prefix}-litter",
            Name = "Litter (lít)"
        };

        var command5 = new CreateUnitCommand
        {
            Code = $"{_prefix}-m",
            Name = "Metter (mét)"
        };

        var unitId1 = await sender.Send(command1, CancellationToken.None);
        var unitId2 = await sender.Send(command2, CancellationToken.None);
        var unitId3 = await sender.Send(command3, CancellationToken.None);
        var unitId4 = await sender.Send(command4, CancellationToken.None);
        var unitId5 = await sender.Send(command5, CancellationToken.None);

        Assert.True(unitId1 > 0);
        Assert.True(unitId2 > 0);
        Assert.True(unitId3 > 0);
        Assert.True(unitId4 > 0);
        Assert.True(unitId5 > 0);

        _unitIds.Add(unitId1);
        _unitIds.Add(unitId2);
        _unitIds.Add(unitId3);
        _unitIds.Add(unitId4);
        _unitIds.Add(unitId5);

        await dbSession.Connection.ExecuteAsync(@"
        UPDATE units SET active = false
        WHERE unit_id = @UnitId
        ", new { UnitId = unitId1 });
    }

    [Fact]
    public async Task List_Should_Return_All()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery {}, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(_unitIds.Count, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Code = $"{_prefix}-kg" }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-kg");
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Code = _prefix }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(5, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Code = $"{_prefix}-something" }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Name = "Kilogram" }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-kg");
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Name = "gram" }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Name = $"{_prefix}-gramabc" }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Should_Filter_By_All_Fields()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Code = $"{_prefix}-kg", Name = "Kilogram" }
        , CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-kg");
    }

    [Fact]
    public async Task Paginate_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListUnitQuery { PageNum = 1, PageSize = 2, Code = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(2, count1);

        var res2 = await sender.Send(new ListUnitQuery { PageNum = 2, PageSize = 2, Code = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(2, count2);

        var res3 = await sender.Send(new ListUnitQuery { PageNum = 3, PageSize = 2, Code = _prefix}
        , CancellationToken.None);

        Assert.Single(res3.Rows, p => p.Code.StartsWith(_prefix));
    }

    [Fact]
    public async Task Paginate_With_Wrong_Number_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListUnitQuery { PageNum = 0, PageSize = 50, Code = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res1.PageNumer);
        Assert.Equal(50, res1.PageSize);
        Assert.Equal(5, count1);

        var res2 = await sender.Send(new ListUnitQuery { PageNum = 1, PageSize = 0, Code = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res2.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res2.PageSize);
        Assert.Equal(5, count2);

        var res3 = await sender.Send(new ListUnitQuery { PageNum = 0, PageSize = 0, Code = _prefix}
        , CancellationToken.None);
        int count3 = res3.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res3.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res3.PageSize);
        Assert.Equal(5, count3);
    }

    [Fact]
    public async Task Filter_By_Active_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Active = false }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-kg");
    }

    [Fact]
    public async Task Filter_By_Active_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Active = true }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(4, count);
    }
}