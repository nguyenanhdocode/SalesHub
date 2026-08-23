using System.Media;
using Application.Features.Suppliers.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Suppliers;

public class CreateSupplierTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public CreateSupplierTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }
    public static TheoryData<CreateSupplierCommand, string> InvalidCommands => new()
    {
        {
            new CreateSupplierCommand
            {
                Code = null!,
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateSupplierCommand
            {
                Code = "",
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateSupplierCommand
            {
                Code = "Mã sản phảm",
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateSupplierCommand
            {
                Code = new string('C', 251),
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateSupplierCommand
            {
                Code = "Code",
                Name = null!
            },
            "Name"
        },
        {
            new CreateSupplierCommand
            {
                Code = "Code",
                Name = ""
            },
            "Name"
        },
        {
            new CreateSupplierCommand
            {
                Code = "Code",
                Name = new string('N', 501)
            },
            "Name"
        },
        {
            new CreateSupplierCommand
            {
                Code = "Code",
                Name = "Name",
                ContactPerson = new string('C', 51)
            },
            "ContactPerson"
        },
        {
            new CreateSupplierCommand
            {
                Code = "Code",
                Name = "Name",
                Phone = new string('0', 51)
            },
            "Phone"
        },
        {
            new CreateSupplierCommand
            {
                Code = "Code",
                Name = "Name",
                TaxCode = new string('T', 51)
            },
            "TaxCode"
        },
        {
            new CreateSupplierCommand
            {
                Code = "Code",
                Name = "Name",
                Email = new string('E', 256)
            },
            "Email"
        },
        {
            new CreateSupplierCommand
            {
                Code = "Code",
                Name = "Name",
                Address = new string('A', 256)
            },
            "Address"
        },
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Create_Should_Validator_Fail(CreateSupplierCommand command, string expectedProperty)
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
        var code = Guid.NewGuid().ToString("N")[..25];
        var command = new CreateSupplierCommand
        {
            Code = code.ToString(),
            Name = $"Nhà cung cấp {code}",
            ContactPerson = "Nguyễn Văn A",
            Phone = "0123456789",
            TaxCode = "9876543210",
            Email = $"{code}@gmail.com",
            Address = "Lê Văn Lương, xã Nhơn Đức, huyện Nhà Bè, Tp.HCM"
        };

        try
        {

            var res = await sender.Send(command, CancellationToken.None);

            Assert.True(res > 0);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT supplier_id FROM suppliers WHERE code = @Code
            AND Name = @Name AND contact_person = @ContactPerson AND phone = @Phone
            AND tax_code = @TaxCode AND email = @Email AND address = @Address
            ", command);

            Assert.True(res == testId);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(
                "DELETE FROM suppliers WHERE code = @Code",
                new { command.Code });
        }
    }

    [Fact]
    public async Task Create_Should_Throw_Fk_Vilolation()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var code = Guid.NewGuid().ToString("N")[..25];
        var command = new CreateSupplierCommand
        {
            Code = code.ToString(),
            Name = $"Nhà cung cấp {code}",
            ContactPerson = "Nguyễn Văn A",
            Phone = "0123456789",
            TaxCode = "9876543210",
            Email = $"{code}@gmail.com",
            Address = "Lê Văn Lương, xã Nhơn Đức, huyện Nhà Bè, Tp.HCM"
        };

        try
        {

            var res = await sender.Send(command, CancellationToken.None);

            var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await sender.Send(command, CancellationToken.None);
            });

            Assert.True(ex.SqlState == PostgresErrorCodes.UniqueViolation);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(
                "DELETE FROM suppliers WHERE code = @Code",
                new { command.Code });
        }
    }
}
