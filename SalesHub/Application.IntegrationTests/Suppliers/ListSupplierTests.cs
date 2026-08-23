using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Suppliers.Delete;
using Application.Features.Suppliers.List;
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

    public ListSupplierTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task DisposeAsync()
    {
        using var scope = _fixture.CreateScope();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        await dbSession.Connection.ExecuteAsync(@"DELETE FROM suppliers WHERE code = ANY(@Codes)"
            , new
            {
                Codes = new[] {
                "LIST-TEST-SUP001"
               , "LIST-TEST-SUP002"
               , "LIST-TEST-SUP003"
               , "LIST-TEST-SUP004"
               , "LIST-TEST-SUP005"
               , "LIST-TEST-SUP006"
               }
            });
    }

    public async Task InitializeAsync()
    {
        var command1 = new CreateSupplierCommand
        {
            Code = "LIST-TEST-SUP001",
            Name = "ABC Pharma",
            ContactPerson = "Nguyễn Văn A",
            Address = "HCM",
            Email = "abc@test.com",
            Phone = "0900000001",
            TaxCode = "TAX001"
        };

        var command2 = new CreateSupplierCommand
        {
            Code = "LIST-TEST-SUP002",
            Name = "ABC Pharma",
            ContactPerson = "Nguyễn Văn B",
            Address = "HCM",
            Email = "abc2@test.com",
            Phone = "0900000002",
            TaxCode = "TAX002"
        };

        var command3 = new CreateSupplierCommand
        {
            Code = "LIST-TEST-SUP003",
            Name = "DEF Pharma",
            ContactPerson = "Nguyễn Văn A",
            Address = "HÀ NỘI",
            Email = "def@test.com",
            Phone = "0900000003",
            TaxCode = "TAX003"
        };

        var command4 = new CreateSupplierCommand
        {
            Code = "LIST-TEST-SUP004",
            Name = "XYZ Pharma",
            ContactPerson = "Nguyễn Văn C",
            Address = "HÀ NỘI",
            Email = "def@test.com",
            Phone = "0900000004",
            TaxCode = "TAX004"
        };

        var command5 = new CreateSupplierCommand
        {
            Code = "LIST-TEST-SUP005",
            Name = "ABC Medical",
            ContactPerson = "Trần Văn A",
            Address = "Đà Nẵng",
            Email = "abc3@test.com",
            Phone = "0900000005",
            TaxCode = "TAX005"
        };

        var command6 = new CreateSupplierCommand
        {
            Code = "LIST-TEST-SUP006",
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

        var res1 = await sender.Send(command1, CancellationToken.None);
        var res2 = await sender.Send(command2, CancellationToken.None);
        var res3 = await sender.Send(command3, CancellationToken.None);
        var res4 = await sender.Send(command4, CancellationToken.None);
        var res5 = await sender.Send(command5, CancellationToken.None);
        var res6 = await sender.Send(command6, CancellationToken.None);

        Assert.True(res1 > 0);
        Assert.True(res2 > 0);
        Assert.True(res3 > 0);
        Assert.True(res4 > 0);
        Assert.True(res5 > 0);
        Assert.True(res6 > 0);
    }

    #region Single field test
    [Fact]
    public async Task List_Should_Return_All()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery(), CancellationToken.None);

        Assert.Equal(6, res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count());
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_One()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Code = "LIST-TEST-SUP001"
        }, CancellationToken.None);

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 1);
        Assert.Equal("LIST-TEST-SUP001", res.Rows.First().Code);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Many()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Code = "LIST-TEST-SUP"
        }, CancellationToken.None);

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 6);
    }

    [Fact]
    public async Task Filter_By_Code_Should_Return_Empty()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Code = "LIST-TEST-SUP-SOMETHING"
        }, CancellationToken.None);

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 0);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 1);
        Assert.Equal("LIST-TEST-SUP006", res.Rows.First().Code);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 4);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 0);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 1);
        Assert.Equal("LIST-TEST-SUP006", res.Rows.First().Code);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 2);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 0);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 1);
        Assert.Equal("LIST-TEST-SUP006", res.Rows.First().Code);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 6);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 0);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 1);
        Assert.Equal("LIST-TEST-SUP001", res.Rows.First().Code);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 3);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 0);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 1);
        Assert.Equal("LIST-TEST-SUP006", res.Rows.First().Code);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 2);
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

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 0);
    }
    #endregion

    #region Field combine tests
    [Fact]
    public async Task Should_Filter_By_All()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var res = await sender.Send(new ListSupplierQuery()
        {
            Code = "LIST-TEST-SUP006",
            Name = "DEF Medical",
            ContactPerson = "Trần Văn B",
            Address = "Huế",
            Email = "def2@test.com",
            Phone = "0900000006",
            TaxCode = "TAX006"
        }, CancellationToken.None);

        Assert.True(res.Rows.Where(p => p.Code.StartsWith("LIST-TEST-")).Count() == 1);
        Assert.Equal("LIST-TEST-SUP006", res.Rows.First().Code);
    }
    #endregion
}
