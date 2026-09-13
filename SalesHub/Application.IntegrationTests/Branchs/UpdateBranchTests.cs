using System.Media;
using Application.Exceptions;
using Application.Features.Branchs.Create;
using Application.Features.Branchs.Update;
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

public class UpdateBranchTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public UpdateBranchTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    public static TheoryData<UpdateBranchCommand, string> InvalidCommands => new()
    {
        {
            new UpdateBranchCommand
            {
                Code = null!,
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateBranchCommand
            {
                Code = "",
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateBranchCommand
            {
                Code = new string('C', 101),
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateBranchCommand
            {
                Code = "Code@#$",
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateBranchCommand
            {
                Code = "Code",
                Name = null!
            },
            "Name"
        },
        {
            new UpdateBranchCommand
            {
                Code = "Code",
                Name = ""
            },
            "Name"
        },
        {
            new UpdateBranchCommand
            {
                Code = "Code",
                Name = new string('N', 251)
            },
            "Name"
        },
        {
            new UpdateBranchCommand
            {
                Code = "Code",
                Name = "Name",
                Address = new string('A', 251)
            },
            "Address"
        },
        {
            new UpdateBranchCommand
            {
                Code = "Code",
                Name = "Name",
                Phone = new string('0', 51)
            },
            "Phone"
        },
        {
            new UpdateBranchCommand
            {
                Code = "Code",
                Name = "Name",
                Email = new string('E', 251)
            },
            "Email"
        },
        {
            new UpdateBranchCommand
            {
                Code = "Code",
                Name = "Name",
                TaxCode = new string('T', 51)
            },
            "TaxCode"
        },
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Update_Should_Throw_Validator_Exception(UpdateBranchCommand command, string expectedProperty)
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await sender.Send(command, CancellationToken.None);
        });

        Assert.Contains(exception.Errors, x => x.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task Update_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataSeed = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        var code = Guid.NewGuid().ToString("N")[..25];
        int branchId = 0;

        try
        {
            var command = new CreateBranchCommand
            {
                Code = code,
                Name = $"{code}name",
                Address = $"{code}address",
                Email = $"{code}@gmail.com",
                Phone = "0000000000",
                TaxCode = $"{code}tax"
            };

            branchId = await sender.Send(command, CancellationToken.None);
            Assert.True(branchId > 0);

            var updateCommand = new UpdateBranchCommand
            {
                BranchId = branchId,
                Code = $"{code}-updated",
                Name = $"{code}name-updated",
                Address = $"{code}address-updated",
                Email = $"{code}-updated@gmail.com",
                Phone = "00000000001",
                TaxCode = $"{code}tax-updated"
            };
            await sender.Send(updateCommand, CancellationToken.None);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT branch_id FROM branchs
            WHERE code = @Code AND name = @Name AND address = @Address
            AND email = @Email AND phone = @Phone AND tax_code = @TaxCode
            ", updateCommand);

            Assert.Equal(branchId, testId);
        }
        finally
        {
            await dataSeed.DeleteBranch(branchId);
        }
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }
}
