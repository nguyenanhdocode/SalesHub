using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Units.Delete;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Units;

public class DeleteUnitTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public DeleteUnitTests(ApplicationFixture fixture)
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

        var command = new CreateUnitCommand
        {
            Code = code,
            Name = $"Đơn vị tính {code}"
        };

        try
        {
            var insertedId = await sender.Send(command, CancellationToken.None);
            Assert.True(insertedId > 0);

            await sender.Send(new DeleteUnitCommand { UnitId = insertedId }, CancellationToken.None);

            int count = await dbSession.Connection.ExecuteScalarAsync<int>(@"SELECT COUNT(1) FROM units WHERE unit_id = @UnitId"
                , new { UnitId = insertedId });

            Assert.Equal(0, count);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"DELETE FROM units WHERE code = @Code", new
            {
                Code = code
            });
        }
    }

    [Fact]
    public async Task Delete_Should_Throw_Fk_Violation()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var code = Guid.NewGuid().ToString("N")[..25];

        var command = new CreateUnitCommand
        {
            Code = code,
            Name = $"Đơn vị tính {code}"
        };

        try
        {
            var insertedUnitId = await sender.Send(command, CancellationToken.None);
            Assert.True(insertedUnitId > 0);

            var insertedSupplierId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            INSERT INTO suppliers (code, name) VALUES ('SUP-0001', 'NHA CUNG CAP SP-0001')
            RETURNING supplier_id;
            ");

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
                    'SP-001'
                    , 'SP-001'
                    , 'Sản phẩm SP-001'
                    , 'AVG'
                    , {insertedUnitId}
                    , {insertedSupplierId})
                RETURNING product_id;
                ");

            var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
               await sender.Send(new DeleteUnitCommand { UnitId = insertedUnitId }); 
            });

            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"DELETE FROM products WHERE internal_code = @Code", new
            {
                Code = "SP-001"
            });

            await dbSession.Connection.ExecuteAsync(@"DELETE FROM suppliers WHERE code = @Code", new
            {
                Code = "SUP-0001"
            });

            await dbSession.Connection.ExecuteAsync(@"DELETE FROM units WHERE code = @Code", new
            {
                Code = code
            });
        }
    }
}