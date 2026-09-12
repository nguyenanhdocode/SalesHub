using Application.Features.Products.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Products;

public class CreateProductTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public CreateProductTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<CreateProductCommand, string> InvalidCommands => new()
    {
        {
            new CreateProductCommand
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
            new CreateProductCommand
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
            new CreateProductCommand
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
            new CreateProductCommand
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
            new CreateProductCommand
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
            new CreateProductCommand
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
            new CreateProductCommand
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
            new CreateProductCommand
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
            new CreateProductCommand
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
            new CreateProductCommand
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
        },
        {
            new CreateProductCommand
            {
                InternalCode = "P-001",
                ExternalCode = "EXT-001",
                Name = "Sản phẩm A",
                CostingMethod = "AVG",
                BaseUnitId = 1,
                SupplierId = 1,
                Active = true,
                VatRate = -1
            },
            "VatRate"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Create_Should_Throw_Validator_Exception(CreateProductCommand command, string expectedProperty)
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
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();

        int baseUnitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int insertedId = 0;
        string internalCode = Guid.NewGuid().ToString("N")[..20];
        string externalCode = Guid.NewGuid().ToString("N")[..20];
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
                Active = true,
                UnitIds = new List<int> { unitId, baseUnitId },
                VatRate = 1.1m
            };

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            int actualId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
                SELECT product_id
                FROM products
                WHERE internal_code = @InternalCode
                  AND external_code = @ExternalCode
                  AND name = @Name
                  AND base_unit_id = @BaseUnitId
                  AND supplier_id = @SupplierId
                  AND vat_rate = 1.1;
            ", command);

            Assert.Equal(insertedId, actualId);

            int productUnitCount1 = await dbSession.Connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM product_unit
                WHERE product_id = @ProductId AND unit_id = @UnitId;
            ", new { ProductId = insertedId, UnitId = unitId });

            Assert.Equal(1, productUnitCount1);

            int productUnitCount2 = await dbSession.Connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM product_unit
                WHERE product_id = @ProductId AND unit_id = @UnitId;
            ", new { ProductId = insertedId, UnitId = baseUnitId });

            Assert.Equal(1, productUnitCount2);
        }
        finally
        {
            await dataRand.DeleteProductUnits(insertedId);
            await dataRand.DeleteProduct(insertedId);
            await dataRand.DeleteUnit(baseUnitId);
            await dataRand.DeleteUnit(unitId);
            await dataRand.DeleteSupplier(supplierId);
        }
    }

    [Fact]
    public async Task Create_Should_Throw_Unique_Violation()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();

        int unitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int insertedId = 0;
        string internalCode = Guid.NewGuid().ToString("N")[..20];

        try
        {
            var command = new CreateProductCommand
            {
                InternalCode = internalCode,
                ExternalCode = "EXT-UNIQUE-001",
                Name = "Sản phẩm unique",
                CostingMethod = "AVG",
                BaseUnitId = unitId,
                SupplierId = supplierId,
                Active = true
            };

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await sender.Send(command, CancellationToken.None);
            });

            Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
        }
        finally
        {
            await dataRand.DeleteProductUnits(insertedId);
            await dataRand.DeleteProduct(insertedId);   
            await dataRand.DeleteUnit(unitId);
            await dataRand.DeleteSupplier(supplierId);
        }
    }
}
