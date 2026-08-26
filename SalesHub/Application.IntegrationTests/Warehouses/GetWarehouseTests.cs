using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Warehouses.Create;
using Application.Features.Warehouses.Delete;
using Application.Features.Warehouses.Get;
using Application.Shared;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Warehouses;

public class GetWarehouseTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public GetWarehouseTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Get_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataSeed = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var warehouseCode = Guid.NewGuid().ToString("N")[..25];
        int branchId = 0;
        int warehouseId = 0;

        try
        {
            branchId = await dataSeed.RandomBranch();

            var command = new CreateWarehouseCommand
            {
                Code = warehouseCode,
                Name = warehouseCode,
                BranchId = branchId
            };

            warehouseId = await sender.Send(command, CancellationToken.None);
            Assert.True(warehouseId > 0);

            var res = await sender.Send(new GetWarehouseQuery { WarehouseId = warehouseId }, CancellationToken.None);

            Assert.Equal(warehouseId, res.WarehouseId);
            Assert.Equal(command.Code, res.Code);
            Assert.Equal(command.Name, res.Name);

            int testBranchId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT branch_id FROM branchs WHERE code = @Code AND name = @Name
            ", new
            {
                Code = res.BranchCode,
                Name = res.BranchName                                                                  
            });

            Assert.Equal(command.BranchId, testBranchId);
        }
        finally
        {
            await dataSeed.DeleteWarehouse(warehouseId);
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
           await sender.Send(new GetWarehouseQuery { WarehouseId = int.MaxValue }, CancellationToken.None);
        });

        Assert.Equal("notfound", ex.Code);
    }
}