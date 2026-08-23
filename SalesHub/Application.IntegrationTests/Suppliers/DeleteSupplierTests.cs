using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Suppliers.Delete;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Suppliers;

public class DeleteSupplierTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public DeleteSupplierTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Delete_Should_Success()
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

            await sender.Send(new DeleteSupplierCommand { SupplierId = res });
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(
                "DELETE FROM suppliers WHERE code = @Code",
                new { command.Code });
        }
    }

    [Fact]
    public async Task Delete_Should_Throw_NotFound()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
        {
            await sender.Send(new DeleteSupplierCommand { SupplierId = int.MaxValue });
        });

        Assert.Equal("notfound", ex.Code);
    }

    [Fact]
    public async Task Delete_Should_Throw_Fk_Violation()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var supplierCode = Guid.NewGuid().ToString("N")[..25];
        var productCode = Guid.NewGuid().ToString("N")[..25];
        var unitCode = Guid.NewGuid().ToString("N")[..25];

        try
        {
            int insertedUnitId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO units (code, name)
            VALUES (@Code, @Name)
            RETURNING unit_id;
            ", new
            {
                Code = unitCode,
                Name = unitCode
            });

            var command = new CreateSupplierCommand
            {
                Code = supplierCode.ToString(),
                Name = supplierCode.ToString(),
                ContactPerson = supplierCode.ToString(),
                Phone = "0123456789",
                TaxCode = "9876543210",
                Email = $"{supplierCode}@gmail.com",
                Address = supplierCode.ToString(),
            };

            int insertedSupplierId = await sender.Send(command, CancellationToken.None);
            
            var insertedProductId = await dbSession.Connection.ExecuteScalarAsync<int>(
                @$"
                INSERT INTO public.products(
                    internal_code
                    , external_code
                    , name
                    , costing_method
                    , base_unit_id
                    , supplier_id)
                VALUES (
                    @InternalCode
                    , @ExternalCode
                    , @Name
                    , 'AVG'
                    , @BaseUnitId
                    , @SupplierId)
                RETURNING product_id;
                ", new
                {
                    InternalCode = productCode,
                    ExternalCode = productCode,
                    Name = productCode,
                    BaseUnitId = insertedUnitId,
                    SupplierId = insertedSupplierId
                });

            var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await sender.Send(new DeleteSupplierCommand { SupplierId =  insertedSupplierId});
            });

            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"DELETE FROM products WHERE internal_code = @Code", new
            {
                Code = productCode
            });

            await dbSession.Connection.ExecuteAsync(@"DELETE FROM suppliers WHERE code = @Code", new
            {
                Code = supplierCode
            });

            await dbSession.Connection.ExecuteAsync(@"DELETE FROM units WHERE code = @Code", new
            {
                Code = unitCode
            });
        }
    }
}
