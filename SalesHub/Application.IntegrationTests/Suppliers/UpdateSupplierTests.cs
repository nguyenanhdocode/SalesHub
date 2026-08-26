using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Suppliers.Delete;
using Application.Features.Suppliers.Update;
using Application.Features.Units.Create;
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
    public async Task Update_Should_Throw_Validation_Exception(UpdateSupplierCommand command, string expectedProperty)
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
        var code = Guid.NewGuid().ToString("N")[..25];
        int insertedId = 0;

        var command = new CreateSupplierCommand
        {
            Code = code.ToString(),
            Name = $"{code}name",
            ContactPerson = $"{code}contactperson",
            Phone = "0300000000",
            TaxCode = "0400000000",
            Email = $"{code}@gmail.com",
            Address = $"{code}address"
        };

        try
        {

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.True(insertedId > 0);

            var updateCommand = new UpdateSupplierCommand
            {
                SupplierId = insertedId,
                Code = $"{code.ToString()}updated",
                Name = $"{code}name-updated",
                ContactPerson = $"{code}contactperson-updated",
                Phone = "0300000001",
                TaxCode = "0400000001",
                Email = $"{code}updated@gmail.com",
                Address = $"{code}address-updated"
            };

            await sender.Send(updateCommand, CancellationToken.None);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT supplier_id FROM suppliers WHERE code = @Code
            AND Name = @Name AND contact_person = @ContactPerson AND phone = @Phone
            AND tax_code = @TaxCode AND email = @Email AND address = @Address
            ", updateCommand);

            Assert.Equal(insertedId, testId);
        }
        finally
        {
            await dataRand.DeleteSupplier(insertedId);
        }
    }
}