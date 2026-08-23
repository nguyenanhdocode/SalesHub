using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Suppliers.Delete;
using Application.Features.Suppliers.Update;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Suppliers;

public class UpdateSupplierTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public UpdateSupplierTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<UpdateSupplierCommand, string> InvalidCommands => new()
    {
        {
            new UpdateSupplierCommand
            {
                Code = null!,
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateSupplierCommand
            {
                Code = "",
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateSupplierCommand
            {
                Code = "Mã sản phảm",
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateSupplierCommand
            {
                Code = new string('C', 251),
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateSupplierCommand
            {
                Code = "Code",
                Name = null!
            },
            "Name"
        },
        {
            new UpdateSupplierCommand
            {
                Code = "Code",
                Name = ""
            },
            "Name"
        },
        {
            new UpdateSupplierCommand
            {
                Code = "Code",
                Name = new string('N', 501)
            },
            "Name"
        },
        {
            new UpdateSupplierCommand
            {
                Code = "Code",
                Name = "Name",
                ContactPerson = new string('C', 51)
            },
            "ContactPerson"
        },
        {
            new UpdateSupplierCommand
            {
                Code = "Code",
                Name = "Name",
                Phone = new string('0', 51)
            },
            "Phone"
        },
        {
            new UpdateSupplierCommand
            {
                Code = "Code",
                Name = "Name",
                TaxCode = new string('T', 51)
            },
            "TaxCode"
        },
        {
            new UpdateSupplierCommand
            {
                Code = "Code",
                Name = "Name",
                Email = new string('E', 256)
            },
            "Email"
        },
        {
            new UpdateSupplierCommand
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
    public async Task Create_Should_Validator_Fail(UpdateSupplierCommand command, string expectedProperty)
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
        var newCode = Guid.NewGuid().ToString("N")[..25];

        try
        {
            // Arange
            var res = await sender.Send(command, CancellationToken.None);
            Assert.True(res > 0);

            var updateCommand = new UpdateSupplierCommand
            {
                SupplierId = res,
                Code = newCode,
                Name = $"Nhà cung cấp {code} updated",
                ContactPerson = "Nguyễn Văn A updated",
                Phone = "0123456780",
                TaxCode = "9876543211",
                Email = $"{code}updated@gmail.com",
                Address = "Lê Văn Lương, xã Nhơn Đức, huyện Nhà Bè, Tp.HCM updated"
            };

            await sender.Send(updateCommand, CancellationToken.None);

            int id = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT supplier_id
            FROM suppliers
            WHERE code = @Code AND name = @Name AND contact_person = @ContactPerson
            AND phone = @Phone AND tax_code = @TaxCode AND email = @Email
            AND address = @Address
            ", updateCommand);

            Assert.Equal(res, id);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(
                "DELETE FROM suppliers WHERE code = ANY(@Codes)",
                new { Codes = new[] { code, newCode } });
        }
    }

    [Fact]
    public async Task Update_Should_Throw_Duplicate_Exception()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        
        var code1 = Guid.NewGuid().ToString("N")[..25].ToString();
        var command1 = new CreateSupplierCommand
        {
            Code = code1,
            Name = $"Nhà cung cấp {code1}",
            ContactPerson = "Nguyễn Văn A",
            Phone = "0123456789",
            TaxCode = "9876543210",
            Email = $"{code1}@gmail.com",
            Address = "Lê Văn Lương, xã Nhơn Đức, huyện Nhà Bè, Tp.HCM"
        };

        var code2 = Guid.NewGuid().ToString("N")[..25].ToString();
        var command2 = new CreateSupplierCommand
        {
            Code = code2,
            Name = $"Nhà cung cấp {code2}",
            ContactPerson = "Nguyễn Văn A",
            Phone = "0123456789",
            TaxCode = "9876543210",
            Email = $"{code2}@gmail.com",
            Address = "Lê Văn Lương, xã Nhơn Đức, huyện Nhà Bè, Tp.HCM"
        };

        try
        {
            // Arange
            var res1 = await sender.Send(command1, CancellationToken.None);
            Assert.True(res1 > 0);

            var res2 = await sender.Send(command2, CancellationToken.None);
            Assert.True(res2 > 0);

            var updateCommand = new UpdateSupplierCommand
            {
                SupplierId = res1,
                Code = command2.Code,
                Name = $"Nhà cung cấp {command2.Code} updated",
                ContactPerson = "Nguyễn Văn A updated",
                Phone = "0123456780",
                TaxCode = "9876543211",
                Email = $"{command2.Code}updated@gmail.com",
                Address = "Lê Văn Lương, xã Nhơn Đức, huyện Nhà Bè, Tp.HCM updated"
            };

            var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
               await sender.Send(updateCommand, CancellationToken.None); 
            });

            // Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(
                "DELETE FROM suppliers WHERE code = ANY(@Codes)",
                new { Codes = new[] { code1, code2 } });
        }
    }
}
