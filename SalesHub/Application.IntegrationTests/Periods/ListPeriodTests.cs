using System.Media;
using Application.Features.Periods.Create;
using Application.Features.Periods.List;
using Application.Features.Suppliers.Create;
using Application.Shared;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Periods;

public class ListPeriodTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly List<int> _periodIds = [];
    private readonly string _prefix = Guid.NewGuid().ToString("N")[..25];

    public ListPeriodTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task DisposeAsync()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();

        foreach (int id in _periodIds)
        {
            await dataRand.DeletePeriod(id);
        }
    }

    public async Task InitializeAsync()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var command1 = new CreatePeriodCommand
        {
            Code = $"{_prefix}-Q1Y2026",
            Name = "Kỳ kế toán Quý I Năm 2026",
            FromDate = new DateTime(2026, 01, 01),
            ToDate = new DateTime(2026, 03, 01)
        };

        var command2 = new CreatePeriodCommand
        {
            Code = $"{_prefix}-Q2Y2026",
            Name = "Kỳ kế toán Quý II Năm 2026",
            FromDate = new DateTime(2026, 04, 01),
            ToDate = new DateTime(2026, 06, 30)
        };

        var command3 = new CreatePeriodCommand
        {
            Code = $"{_prefix}-Q3Y2026",
            Name = "Kỳ kế toán Quý III Năm 2026",
            FromDate = new DateTime(2026, 07, 01),
            ToDate = new DateTime(2026, 09, 30)
        };

        var command4 = new CreatePeriodCommand
        {
            Code = $"{_prefix}-Q4Y2026",
            Name = "Kỳ kế toán Quý IV Năm 2026",
            FromDate = new DateTime(2026, 10, 01),
            ToDate = new DateTime(2026, 12, 31)
        };

        int periodId1 = await sender.Send(command1, CancellationToken.None);
        int periodId2 = await sender.Send(command2, CancellationToken.None);
        int periodId3 = await sender.Send(command3, CancellationToken.None);
        int periodId4 = await sender.Send(command4, CancellationToken.None);

        Assert.NotEqual(0, periodId1);
        Assert.NotEqual(0, periodId2);
        Assert.NotEqual(0, periodId3);
        Assert.NotEqual(0, periodId4);

        _periodIds.Add(periodId1);
        _periodIds.Add(periodId2);
        _periodIds.Add(periodId3);
        _periodIds.Add(periodId4);

        await dbSession.Connection.ExecuteAsync(@"
        UPDATE periods SET is_closed = true WHERE period_id = @PeriodId
        ", new  { PeriodId = periodId1 });
    }

    [Fact]
    public async Task List_Should_Return_All()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListPeriodQuery() {}, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListPeriodQuery()
        {
            Code = $"{_prefix}-Q1Y2026"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q1Y2026");
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListPeriodQuery()
        {
            Code = $"Y2026"
        }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListPeriodQuery()
        {
            Code = $"{_prefix}SOMETHING"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    // Name
    [Fact]
    public async Task Filter_By_Name_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListPeriodQuery()
        {
            Name = $"kế toán Quý III Năm 2026"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q3Y2026");
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListPeriodQuery()
        {
            Name = $"kế toán"
        }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListPeriodQuery()
        {
            Name = $"{_prefix}SOMETHING"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    // IsClosed
    [Fact]
    public async Task Filter_By_IsClosed_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListPeriodQuery()
        {
            IsClosed = true
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q1Y2026");
    }

    [Fact]
    public async Task Filter_By_IsClosed_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListPeriodQuery()
        {
            IsClosed = false
        }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Paginate_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListPeriodQuery { PageNum = 1, PageSize = 3, Code = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(3, count1);

        var res2 = await sender.Send(new ListPeriodQuery { PageNum = 2, PageSize = 3, Code = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, count2);
    }

    [Fact]
    public async Task Paginate_With_Wrong_Number_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListPeriodQuery { PageNum = 0, PageSize = 50, Code = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res1.PageNumer);
        Assert.Equal(50, res1.PageSize);
        Assert.Equal(4, count1);

        var res2 = await sender.Send(new ListPeriodQuery { PageNum = 1, PageSize = 0, Code = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res2.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res2.PageSize);
        Assert.Equal(4, count2);

        var res3 = await sender.Send(new ListPeriodQuery { PageNum = 0, PageSize = 0}
        , CancellationToken.None);
        int count3 = res3.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res3.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res3.PageSize);
        Assert.Equal(4, count3);
    }
}