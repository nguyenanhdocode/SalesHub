using System.Media;
using Application.Exceptions;
using Application.Features.Branchs.Create;
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

public class CreateBranchTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public CreateBranchTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<CreateBranchCommand, string> InvalidCommands => new()
    {
        {
            new CreateBranchCommand
            {
                Code = null!,
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateBranchCommand
            {
                Code = "",
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateBranchCommand
            {
                Code = new string('C', 101),
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateBranchCommand
            {
                Code = "Code@#$",
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateBranchCommand
            {
                Code = "Code",
                Name = null!
            },
            "Name"
        },
        {
            new CreateBranchCommand
            {
                Code = "Code",
                Name = ""
            },
            "Name"
        },
        {
            new CreateBranchCommand
            {
                Code = "Code",
                Name = new string('N', 251)
            },
            "Name"
        },
        {
            new CreateBranchCommand
            {
                Code = "Code",
                Name = "Name",
                Address = new string('A', 251)
            },
            "Address"
        },
        {
            new CreateBranchCommand
            {
                Code = "Code",
                Name = "Name",
                Phone = new string('0', 51)
            },
            "Phone"
        },
        {
            new CreateBranchCommand
            {
                Code = "Code",
                Name = "Name",
                Email = new string('E', 251)
            },
            "Email"
        },
        {
            new CreateBranchCommand
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
    public async Task Create_Should_Throw_Validator_Exception(CreateBranchCommand command, string expectedProperty)
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

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT branch_id FROM branchs
            WHERE code = @Code AND name = @Name AND address = @Address
            AND email = @Email AND phone = @Phone AND tax_code = @TaxCode
            ", command);

            Assert.Equal(branchId, testId);
        }
        finally
        {
            await dataSeed.DeleteBranch(branchId);
        }
    }
}
