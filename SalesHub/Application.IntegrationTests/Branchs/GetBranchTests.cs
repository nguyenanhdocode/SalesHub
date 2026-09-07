using System.Media;
using Application.Exceptions;
using Application.Features.Branchs.Create;
using Application.Features.Branchs.Get;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Warehouses.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Branchs;

public class GetBranchTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public GetBranchTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataSeed = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var code = Guid.NewGuid().ToString("N")[..25];
        int branchId = 0;

        try
        {
            var command = new CreateBranchCommand
            {
                Code = code,
                Name = code,
                Address = $"{code}address",
                Email = $"{code}@gmail.com",
                Phone = "0000000000",
                TaxCode = code
            };

            branchId = await sender.Send(command, CancellationToken.None);
            Assert.True(branchId > 0);

            var res = await sender.Send(new GetBranchQuery { BranchId = branchId }, CancellationToken.None);

            Assert.Equal(branchId, res.BranchId);
            Assert.Equal(command.Code, res.Code);
            Assert.Equal(command.Name, res.Name);
            Assert.Equal(command.Address, res.Address);
            Assert.Equal(command.Email, res.Email);
            Assert.Equal(command.Phone, res.Phone);
            Assert.Equal(command.TaxCode, res.TaxCode);
        }
        finally
        {
            await dataSeed.DeleteBranch(branchId);
        }
    }

    [Fact]
    public async Task Get_Should_Throw_NotFound()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
        {
           await sender.Send(new GetBranchQuery { BranchId = int.MaxValue }, CancellationToken.None); 
        });

        Assert.Equal("notfound", ex.Code);
    }
}