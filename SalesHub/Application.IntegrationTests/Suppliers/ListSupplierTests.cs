using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Suppliers.Delete;
using Application.Features.Suppliers.List;
using Application.Shared;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Suppliers;

public class ListSupplierTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly string _prefix = Guid.NewGuid().ToString("N")[..25];
    private readonly List<int> _supplierIds = [];

    public ListSupplierTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task DisposeAsync()
    {
        using var scope = _fixture.CreateScope();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        foreach (int id in _supplierIds)
        {
            await dataRand.DeleteSupplier(id);
        }
    }

    public async Task InitializeAsync()
    {
        var command1 = new CreateSupplierCommand
        {
            Code = $"{_prefix}SUP001",
            Name = "ABC Pharma",
            ContactPerson = "Nguyễn Văn A",
            Address = "HCM",
            Email = "abc@test.com",
            Phone = "0900000001",
            TaxCode = "TAX001"
        };

        var command2 = new CreateSupplierCommand
        {
            Code = $"{_prefix}SUP002",
            Name = "ABC Pharma",
            ContactPerson = "Nguyễn Văn B",
            Address = "HCM",
            Email = "abc2@test.com",
            Phone = "0900000002",
            TaxCode = "TAX002"
        };

        var command3 = new CreateSupplierCommand
        {
            Code = $"{_prefix}SUP003",
            Name = "DEF Pharma",
            ContactPerson = "Nguyễn Văn A",
            Address = "HÀ NỘI",
            Email = "def@test.com",
            Phone = "0900000003",
            TaxCode = "TAX003"
        };

        var command4 = new CreateSupplierCommand
        {
            Code = $"{_prefix}SUP004",
            Name = "XYZ Pharma",
            ContactPerson = "Nguyễn Văn C",
            Address = "HÀ NỘI",
            Email = "def@test.com",
            Phone = "0900000004",
            TaxCode = "TAX004"
        };

        var command5 = new CreateSupplierCommand
        {
            Code = $"{_prefix}SUP005",
            Name = "ABC Medical",
            ContactPerson = "Trần Văn A",
            Address = "Đà Nẵng",
            Email = "abc3@test.com",
            Phone = "0900000005",
            TaxCode = "TAX005"
        };

        var command6 = new CreateSupplierCommand
        {
            Code = $"{_prefix}SUP006",
            Name = "DEF Medical",
            ContactPerson = "Trần Văn B",
            Address = "Huế",
            Email = "def2@test.com",
            Phone = "0900000006",
            TaxCode = "TAX006"
        };

        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        int supplierId1 = await sender.Send(command1, CancellationToken.None);
        int supplierId2 = await sender.Send(command2, CancellationToken.None);
        int supplierId3 = await sender.Send(command3, CancellationToken.None);
        int supplierId4 = await sender.Send(command4, CancellationToken.None);
        int supplierId5 = await sender.Send(command5, CancellationToken.None);
        int supplierId6 = await sender.Send(command6, CancellationToken.None);

        Assert.True(supplierId1 > 0);
        Assert.True(supplierId2 > 0);
        Assert.True(supplierId3 > 0);
        Assert.True(supplierId4 > 0);
        Assert.True(supplierId5 > 0);
        Assert.True(supplierId6 > 0);

        _supplierIds.Add(supplierId1);
        _supplierIds.Add(supplierId2);
        _supplierIds.Add(supplierId3);
        _supplierIds.Add(supplierId4);
        _supplierIds.Add(supplierId5);
        _supplierIds.Add(supplierId6);
    }

    [Fact]
    public async Task List_Should_Return_All()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery(), CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(6, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Code = $"{_prefix}SUP001"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}SUP001");
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Code = $"{_prefix}SUP"
        }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(6, count);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Code = $"{_prefix}SOMETHING"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Name = "DEF Medical"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}SUP006");
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Name = "Pharma"
        }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(4, count);
    }

    [Fact]
    public async Task Filter_By_Name_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Name = "GHI Pharma"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_ContactPerson_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            ContactPerson = "Trần Văn B"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}SUP006");
    }

    [Fact]
    public async Task Filter_By_ContactPerson_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            ContactPerson = "Trần Văn"
        }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Filter_By_ContactPerson_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            ContactPerson = "Trần Văn F"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_TaxCode_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            TaxCode = "TAX006"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}SUP006");
    }

    [Fact]
    public async Task Filter_By_TaxCode_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            TaxCode = "TAX"
        }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(6, count);
    }

    [Fact]
    public async Task Filter_By_TaxCode_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            TaxCode = "TAX9999"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_Email_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Email = "abc@test.com"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}SUP001");
    }

    [Fact]
    public async Task Filter_By_Email_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Email = "abc"
        }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Filter_By_Email_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Email = "abcpharma@gmail.com"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Filter_By_Address_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Address = "Huế"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}SUP006");
    }

    [Fact]
    public async Task Filter_By_Address_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Address = "HCM"
        }, CancellationToken.None);
        int count = res.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Filter_By_Address_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Email = "Tây Ninh"
        }, CancellationToken.None);

        Assert.Empty(res.Rows.Where(p => p.Code.StartsWith(_prefix)).ToList());
    }

    [Fact]
    public async Task Should_Filter_By_All()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Code = $"{_prefix}SUP006",
            Name = "DEF Medical",
            ContactPerson = "Trần Văn B",
            Address = "Huế",
            Email = "def2@test.com",
            Phone = "0900000006",
            TaxCode = "TAX006"
        }, CancellationToken.None);

        Assert.Single(res.Rows, p => p.Code == $"{_prefix}SUP006");
    }

    [Fact]
    public async Task Paginate_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListSupplierQuery { PageNum = 1, PageSize = 3, Code = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(3, count1);

        var res2 = await sender.Send(new ListSupplierQuery { PageNum = 2, PageSize = 3, Code = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(3, count2);
    }

    [Fact]
    public async Task Paginate_With_Wrong_Number_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var res1 = await sender.Send(new ListSupplierQuery { PageNum = 0, PageSize = 50, Code = _prefix}
        , CancellationToken.None);
        int count1 = res1.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res1.PageNumer);
        Assert.Equal(50, res1.PageSize);
        Assert.Equal(6, count1);

        var res2 = await sender.Send(new ListSupplierQuery { PageNum = 1, PageSize = 0, Code = _prefix}
        , CancellationToken.None);
        int count2 = res2.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res2.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res2.PageSize);
        Assert.Equal(6, count2);

        var res3 = await sender.Send(new ListSupplierQuery { PageNum = 0, PageSize = 0}
        , CancellationToken.None);
        int count3 = res3.Rows.Where(p => p.Code.StartsWith(_prefix)).Count();

        Assert.Equal(1, res3.PageNumer);
        Assert.Equal(Constants.PAGE_SIZE, res3.PageSize);
        Assert.Equal(6, count3);
    }
}
