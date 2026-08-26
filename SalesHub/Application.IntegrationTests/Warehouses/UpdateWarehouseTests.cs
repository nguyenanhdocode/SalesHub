using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Warehouses.Create;
using Application.Features.Warehouses.Update;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Warehouses;

public class UpdateWarehouseTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;
    public UpdateWarehouseTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<UpdateWarehouseCommand, string> InvalidCommands => new()
    {
        {
            new UpdateWarehouseCommand
            {
                Code = null!,
                Name = "Name",
                BranchId = 1
            },
            "Code"
        },
        {
            new UpdateWarehouseCommand
            {
                Code = "",
                Name = "Name",
                BranchId = 1
            },
            "Code"
        },
        {
            new UpdateWarehouseCommand
            {
                Code = new string('C', 101),
                Name = "Name",
                BranchId = 1
            },
            "Code"
        },
        {
            new UpdateWarehouseCommand
            {
                Code = "Code",
                Name = null!,
                BranchId = 1
            },
            "Name"
        },
        {
            new UpdateWarehouseCommand
            {
                Code = "Code",
                Name = "",
                BranchId = 1
            },
            "Name"
        },
        {
            new UpdateWarehouseCommand
            {
                Code = "Code",
                Name = new string('C', 251),
                BranchId = 1
            },
            "Name"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Update_Should_Throw_Validator_Exception(UpdateWarehouseCommand command, string expectedProperty)
    {
        using var scope = _fixture.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await sender.Send(command, CancellationToken.None);
        });

        Assert.Contains(exception.Errors, x => x.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task Update_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        var warehouseCode = Guid.NewGuid().ToString("N")[..25];
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

            var updateCommand = new UpdateWarehouseCommand
            {
                WarehouseId = warehouseId,
                Code = $"{warehouseCode}updated",
                Name = $"{warehouseCode}updated",
                BranchId = branchId,
                Active = false
            };

            await sender.Send(updateCommand, CancellationToken.None);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT warehouse_id FROM warehouses
            WHERE code = @Code AND name = @Name AND branch_id = @BranchId AND active = @Active
            ", updateCommand);

            Assert.Equal(warehouseId, testId);
        }
        finally
        {
            await dataRand.DeleteWarehouse(warehouseId);
            await dataRand.DeleteBranch(branchId);
        }
    }
}