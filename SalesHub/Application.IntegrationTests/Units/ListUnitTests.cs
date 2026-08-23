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

    public ListUnitTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task DisposeAsync()
    {
        using var scope = _fixture.CreateScope();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        await dbSession.Connection.ExecuteAsync(@"DELETE FROM units WHERE code ILIKE 'LIST-TEST-%'");
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var command1 = new CreateUnitCommand
        {
            Code = "LIST-TEST-kg",
            Name = "Kilogram"
        };

        var command2 = new CreateUnitCommand
        {
            Code = "LIST-TEST-gr",
            Name = "Gram"
        };

        var command3 = new CreateUnitCommand
        {
            Code = "LIST-TEST-pcs",
            Name = "Pcs (cái)"
        };

        var command4 = new CreateUnitCommand
        {
            Code = "LIST-TEST-litter",
            Name = "Litter (lít)"
        };

        var command5 = new CreateUnitCommand
        {
            Code = "LIST-TEST-m",
            Name = "Metter (mét)"
        };

        var res1 = await sender.Send(command1, CancellationToken.None);
        var res2 = await sender.Send(command2, CancellationToken.None);
        var res3 = await sender.Send(command3, CancellationToken.None);
        var res4 = await sender.Send(command4, CancellationToken.None);
        var res5 = await sender.Send(command5, CancellationToken.None);

        Assert.True(res1 > 0);
        Assert.True(res2 > 0);
        Assert.True(res3 > 0);
        Assert.True(res4 > 0);
        Assert.True(res5 > 0);
    }

    [Fact]
    public async Task List_Should_Return_All()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery {}, CancellationToken.None);

        Assert.Equal(5, res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count());
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Code = "LIST-TEST-kg" }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == "LIST-TEST-kg");
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Code = "LIST-TEST" }, CancellationToken.None);

        Assert.Equal(5, res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count());
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Code = "LIST-TEST-SOMETHING" }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).ToList());
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Name = "Kilogram" }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == "LIST-TEST-kg");
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Name = "gram" }, CancellationToken.None);

        Assert.Equal(2, res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count());
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Name = "gramabc" }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).ToList());
    }

    [Fact]
    public async Task Should_Filter_By_All()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res = await sender.Send(new ListUnitQuery { Code = "kg", Name = "Kilogram" }
        , CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == "LIST-TEST-kg");
    }

    [Fact]
    public async Task Paginate_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListUnitQuery { PageNum = 1, PageSize = 2}
        , CancellationToken.None);

        Assert.Equal(2, res1.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count());

        var res2 = await sender.Send(new ListUnitQuery { PageNum = 2, PageSize = 2}
        , CancellationToken.None);

        Assert.Equal(2, res2.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count());

        var res3 = await sender.Send(new ListUnitQuery { PageNum = 3, PageSize = 2}
        , CancellationToken.None);

        Assert.Single(res3.Rows, p => p.Code.StartsWith("LIST-TEST-"));
    }

    [Fact]
    public async Task Paginate_With_Wrong_Number_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListUnitQuery { PageNum = 0, PageSize = 50}
        , CancellationToken.None);

        Assert.Equal(1, res1.PageNumer);
        Assert.Equal(50, res1.PageSize);
        Assert.Equal(5, res1.Rows.Count());

        var res2 = await sender.Send(new ListUnitQuery { PageNum = 1, PageSize = 0}
        , CancellationToken.None);

        Assert.Equal(1, res2.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res2.PageSize);
        Assert.Equal(5, res2.Rows.Count());

        var res3 = await sender.Send(new ListUnitQuery { PageNum = 0, PageSize = 0}
        , CancellationToken.None);

        Assert.Equal(1, res3.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res3.PageSize);
        Assert.Equal(5, res3.Rows.Count());
    }
}
