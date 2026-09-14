using Application.Features.Branchs.Create;
using Application.Features.Branchs.List;
using Application.Shared;
using Dapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.IntegrationTests.Branchs;

public class ListBranchTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly List<int> _branchIds = [];
    private readonly string _prefix = Guid.NewGuid().ToString();
    private readonly IServiceScope _scope;

    public ListBranchTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    public async Task DisposeAsync()
    {
        try
        {
            var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();

            foreach (int id in _branchIds)
            {
                await dataRand.DeleteBranch(id);
            }
        }
        finally
        {
            _scope.Dispose();
        }
    }

    public async Task InitializeAsync()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var command1 = new CreateBranchCommand
        {
            Code = $"{_prefix}-Q1",
            Name = "Chi nhánh Quận 1",
            Address = "Hồ Chí Minh",
            Phone = "0901000001",
            Email = "quan1@test.com",
            TaxCode = "TAX-Q1"
        };

        var command2 = new CreateBranchCommand
        {
            Code = $"{_prefix}-Q2",
            Name = "Chi nhánh Gò Vấp",
            Address = "Hồ Chí Minh",
            Phone = "0901000002",
            Email = "govap@test.com",
            TaxCode = "TAX-Q2"
        };

        var command3 = new CreateBranchCommand
        {
            Code = $"{_prefix}-Q3",
            Name = "Chi nhánh Thủ Đức",
            Address = "Đà Nẵng",
            Phone = "0901000003",
            Email = "thuduc@test.com",
            TaxCode = "TAX-Q3"
        };

        var command4 = new CreateBranchCommand
        {
            Code = $"{_prefix}-Q4",
            Name = "Chi nhánh Cần Thơ",
            Address = "Cần Thơ",
            Phone = "0901000004",
            Email = "cantho@test.com",
            TaxCode = "TAX-Q4"
        };

        int branchId1 = await sender.Send(command1, CancellationToken.None);
        int branchId2 = await sender.Send(command2, CancellationToken.None);
        int branchId3 = await sender.Send(command3, CancellationToken.None);
        int branchId4 = await sender.Send(command4, CancellationToken.None);

        Assert.NotEqual(0, branchId1);
        Assert.NotEqual(0, branchId2);
        Assert.NotEqual(0, branchId3);
        Assert.NotEqual(0, branchId4);

        _branchIds.Add(branchId1);
        _branchIds.Add(branchId2);
        _branchIds.Add(branchId3);
        _branchIds.Add(branchId4);
    }

    [Fact]
    public async Task List_Should_Return_All()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery { PageSize = int.MaxValue }, CancellationToken.None);
        int count = res.Rows.Count(p => p.Code.StartsWith(_prefix));

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_One()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Code = $"{_prefix}-Q1"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q1");
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Code = "Q"
        }, CancellationToken.None);
        int count = res.Rows.Count(p => p.Code.StartsWith(_prefix));

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Code = $"{_prefix}-NOPE"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_One()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Name = "Chi nhánh Thủ Đức"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q3");
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Many()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Name = "Chi nhánh"
        }, CancellationToken.None);
        int count = res.Rows.Count(p => p.Code.StartsWith(_prefix));

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Empty()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Name = $"{_prefix}-NOT-FOUND"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_Address_Should_Return_One()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Address = "Cần Thơ"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q4");
    }

    [Fact]
    public async Task Filter_By_Phone_Should_Return_One()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Phone = "0901000002"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q2");
    }

    [Fact]
    public async Task Filter_By_Email_Should_Return_One()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Email = "thuduc@test.com"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q3");
    }

    [Fact]
    public async Task Filter_By_TaxCode_Should_Return_One()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            TaxCode = "TAX-Q4"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q4");
    }

    [Fact]
    public async Task Should_Filter_By_All_Fields()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListBranchQuery
        {
            Code = $"{_prefix}-Q2",
            Name = "Chi nhánh Gò Vấp",
            Address = "Hồ Chí Minh",
            Phone = "0901000002",
            Email = "govap@test.com",
            TaxCode = "TAX-Q2"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}-Q2");
    }

    [Fact]
    public async Task Paginate_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res1 = await sender.Send(new ListBranchQuery { PageNum = 1, PageSize = 2, Code = _prefix }, CancellationToken.None);
        int count1 = res1.Rows.Count(p => p.Code.StartsWith(_prefix));

        Assert.Equal(2, count1);

        var res2 = await sender.Send(new ListBranchQuery { PageNum = 2, PageSize = 2, Code = _prefix }, CancellationToken.None);
        int count2 = res2.Rows.Count(p => p.Code.StartsWith(_prefix));

        Assert.Equal(2, count2);
    }

    [Fact]
    public async Task Paginate_With_Wrong_Number_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var res1 = await sender.Send(new ListBranchQuery { PageNum = 0, PageSize = 50, Code = _prefix }, CancellationToken.None);
        int count1 = res1.Rows.Count(p => p.Code.StartsWith(_prefix));

        Assert.Equal(1, res1.PageNumer);
        Assert.Equal(50, res1.PageSize);
        Assert.Equal(4, count1);

        var res2 = await sender.Send(new ListBranchQuery { PageNum = 1, PageSize = 0, Code = _prefix }, CancellationToken.None);
        int count2 = res2.Rows.Count(p => p.Code.StartsWith(_prefix));

        Assert.Equal(1, res2.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res2.PageSize);
        Assert.Equal(4, count2);

        var res3 = await sender.Send(new ListBranchQuery { PageNum = 0, PageSize = 0, Code = _prefix }, CancellationToken.None);
        int count3 = res3.Rows.Count(p => p.Code.StartsWith(_prefix));

        Assert.Equal(1, res3.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res3.PageSize);
        Assert.Equal(4, count3);
    }
}