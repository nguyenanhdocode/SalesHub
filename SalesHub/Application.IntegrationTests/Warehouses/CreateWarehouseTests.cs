using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Warehouses.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Warehouses;

public class CreateWarehouseTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public CreateWarehouseTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<CreateWarehouseCommand, string> InvalidCommands => new()
    {
        {
            new CreateWarehouseCommand
            {
                Code = null!,
                Name = "Name",
                BranchId = 1
            },
            "Code"
        },
        {
            new CreateWarehouseCommand
            {
                Code = "",
                Name = "Name",
                BranchId = 1
            },
            "Code"
        },
        {
            new CreateWarehouseCommand
            {
                Code = new string('C', 101),
                Name = "Name",
                BranchId = 1
            },
            "Code"
        },
        {
            new CreateWarehouseCommand
            {
                Code = "Code",
                Name = null!,
                BranchId = 1
            },
            "Name"
        },
        {
            new CreateWarehouseCommand
            {
                Code = "Code",
                Name = "",
                BranchId = 1
            },
            "Name"
        },
        {
            new CreateWarehouseCommand
            {
                Code = "Code",
                Name = new string('C', 251),
                BranchId = 1
            },
            "Name"
        },
        {
            new CreateWarehouseCommand
            {
                Code = "Code",
                Name = "Name",
                BranchId = 0
            },
            "BranchId"
        },
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Create_Should_Throw_Validator_Exception(CreateWarehouseCommand command, string expectedProperty)
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
    public async Task Create_Should_Success()
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

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT warehouse_id FROM warehouses
            WHERE code = @Code AND name = @Name AND branch_id = @BranchId
            ", command);

            Assert.Equal(warehouseId, testId);
        }
        finally
        {
            await dataSeed.DeleteWarehouse(warehouseId);
            await dataSeed.DeleteBranch(branchId);
        }
    }
}
