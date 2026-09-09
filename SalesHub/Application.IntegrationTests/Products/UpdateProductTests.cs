using Application.Features.Products.Create;
using Application.Features.Products.Update;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Products;

public class UpdateProductTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public UpdateProductTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<UpdateProductCommand, string> InvalidCommands => new()
    {
        {
            new UpdateProductCommand
            {
                InternalCode = null!,
                ExternalCode = "EXT-001",
                Name = "Sản phẩm A",
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "InternalCode"
        },
        {
            new UpdateProductCommand
            {
                InternalCode = "",
                ExternalCode = "EXT-001",
                Name = "Sản phẩm A",
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "InternalCode"
        },
        {
            new UpdateProductCommand
            {
                InternalCode = new string('P', 51),
                ExternalCode = "EXT-001",
                Name = "Sản phẩm A",
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "InternalCode"
        },
        {
            new UpdateProductCommand
            {
                InternalCode = "PRODUCT!",
                ExternalCode = "EXT-001",
                Name = "Sản phẩm A",
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "InternalCode"
        },
        {
            new UpdateProductCommand
            {
                InternalCode = "P-001",
                ExternalCode = new string('E', 51),
                Name = "Sản phẩm A",
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "ExternalCode"
        },
        {
            new UpdateProductCommand
            {
                InternalCode = "P-001",
                ExternalCode = "EXT!001",
                Name = "Sản phẩm A",
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "ExternalCode"
        },
        {
            new UpdateProductCommand
            {
                InternalCode = "P-001",
                ExternalCode = "EXT-001",
                Name = null!,
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "Name"
        },
        {
            new UpdateProductCommand
            {
                InternalCode = "P-001",
                ExternalCode = "EXT-001",
                Name = "",
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "Name"
        },
        {
            new UpdateProductCommand
            {
                InternalCode = "P-001",
                ExternalCode = "EXT-001",
                Name = new string('N', 251),
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "Name"
        },
        {
            new UpdateProductCommand
            {
                InternalCode = "P-001",
                ExternalCode = "EXT-001",
                Name = "Sản phẩm A",
                CostingMethod = new string('M', 11),
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true
            },
            "CostingMethod"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Update_Should_Throw_Validator_Exception(UpdateProductCommand command, string expectedProperty)
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

        int baseUnitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int insertedId = 0;
        string internalCode = Guid.NewGuid().ToString("N")[..20];
        string externalCode = Guid.NewGuid().ToString("N")[..20];
        int supplierId2 = await dataRand.RandomSupplier();
        int unitId = await dataRand.RandomUnit();

        try
        {
            var command = new CreateProductCommand
            {
                InternalCode = internalCode,
                ExternalCode = externalCode,
                Name = $"{internalCode}-Name",
                CostingMethod = "AVG",
                BaseUnitId = baseUnitId,
                SupplierId = supplierId,
                Active = true
            };

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            var updateCommand = new UpdateProductCommand
            {
                ProductId = insertedId,
                InternalCode = $"{internalCode}-updated",
                ExternalCode = $"{externalCode}-updated",
                Name = $"{internalCode}-Name-updated",
                BaseUnitId = unitId,
                SupplierId = supplierId2,
                Active = false
            };

            await sender.Send(updateCommand, CancellationToken.None);

            int actualId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT product_id FROM products
            WHERE internal_code = @InternalCode AND external_code = @ExternalCode
            AND name = @Name AND base_unit_id = @BaseUnitId AND supplier_id = @SupplierId
            AND active = @Active
            ", updateCommand);

            Assert.Equal(insertedId, actualId);

            int productUnitCount = await dbSession.Connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM product_unit
                WHERE product_id = @ProductId;
            ", new { ProductId = insertedId, UnitId = unitId });

            Assert.Equal(2, productUnitCount);
        }
        finally
        {
            await dataRand.DeleteProductUnits(insertedId);
            await dataRand.DeleteProduct(insertedId);
            await dataRand.DeleteUnit(baseUnitId);
            await dataRand.DeleteSupplier(supplierId);
            await dataRand.DeleteSupplier(supplierId2);
            await dataRand.DeleteUnit(unitId);
        }
    }
}