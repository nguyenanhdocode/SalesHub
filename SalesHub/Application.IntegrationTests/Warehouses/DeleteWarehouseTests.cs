using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Warehouses.Create;
using Application.Features.Warehouses.Delete;
using Application.Shared;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Warehouses;

public class DeleteWarehouseTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public DeleteWarehouseTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Delete_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        var warehouseCode = Guid.NewGuid().ToString();
        int branchId = 0;
        int warehouseId = 0;

        try
        {
            branchId = await dataRand.RandomBranch();

            var command = new CreateWarehouseCommand
            {
                Code = warehouseCode,
                Name = warehouseCode,
                BranchId = branchId
            };

            warehouseId = await sender.Send(command, CancellationToken.None);
            Assert.True(warehouseId > 0);

            await sender.Send(new DeleteWarehouseCommand { WarehouseId = warehouseId }, CancellationToken.None);

            int testCount = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM warehouses WHERE warehouse_id = @WarehouseId
            ", new {WarehouseId = warehouseId});

            Assert.Equal(0, testCount);
        }
        finally
        {
            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
        }
    }
}