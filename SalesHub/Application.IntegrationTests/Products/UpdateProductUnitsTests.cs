using Application.Exceptions;
using Application.Features.Products.Create;
using Application.Features.Products.Units.Update;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Products;

public class UpdateProductUnitsTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public UpdateProductUnitsTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
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
        int unitId1 = await dataRand.RandomUnit();
        int unitId2 = await dataRand.RandomUnit();

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
                UnitIds = new List<int> { baseUnitId, unitId1 }
            };

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            var updateCommand = new UpdateProductUnitsCommand
            {
                ProductId = insertedId,
                UnitIds = new List<int> { baseUnitId, unitId1, unitId2 }
            };

            await sender.Send(updateCommand, CancellationToken.None);

            int productUnitCount = await dbSession.Connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM product_unit
                WHERE product_id = @ProductId;
            ", new { ProductId = insertedId });

            Assert.Equal(3, productUnitCount);
        }
        finally
        {
            await dataRand.DeleteProductUnits(insertedId);
            await dataRand.DeleteProduct(insertedId);
            await dataRand.DeleteUnit(baseUnitId);
            await dataRand.DeleteUnit(unitId1);
            await dataRand.DeleteUnit(unitId2);
            await dataRand.DeleteSupplier(supplierId);
        }
    }

    [Fact]
    public async Task Update_Delete_Many_Should_Success()
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
        int unitId1 = await dataRand.RandomUnit();
        int unitId2 = await dataRand.RandomUnit();

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
                UnitIds = new List<int> { baseUnitId, unitId1, unitId2 }
            };

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            var updateCommand = new UpdateProductUnitsCommand
            {
                ProductId = insertedId,
                UnitIds = new List<int> { baseUnitId }
            };

            await sender.Send(updateCommand, CancellationToken.None);

            int productUnitCount = await dbSession.Connection.ExecuteScalarAsync<int>(@"
                SELECT COUNT(*)
                FROM product_unit
                WHERE product_id = @ProductId;
            ", new { ProductId = insertedId });

            Assert.Equal(1, productUnitCount);
        }
        finally
        {
            await dataRand.DeleteProductUnits(insertedId);
            await dataRand.DeleteProduct(insertedId);
            await dataRand.DeleteUnit(baseUnitId);
            await dataRand.DeleteUnit(unitId1);
            await dataRand.DeleteUnit(unitId2);
            await dataRand.DeleteSupplier(supplierId);
        }
    }

    [Fact]
    public async Task Update_Delete_Should_Throw_Base_Unit_Delete_Restrict()
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
        int unitId1 = await dataRand.RandomUnit();
        int unitId2 = await dataRand.RandomUnit();

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
                UnitIds = new List<int> { baseUnitId, unitId1, unitId2 }
            };

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            var updateCommand = new UpdateProductUnitsCommand
            {
                ProductId = insertedId,
                UnitIds = new List<int> { }
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
               await sender.Send(updateCommand, CancellationToken.None); 
            });

            Assert.Equal("base_unit_delete_restrict", ex.Code);
        }
        finally
        {
            await dataRand.DeleteProductUnits(insertedId);
            await dataRand.DeleteProduct(insertedId);
            await dataRand.DeleteUnit(baseUnitId);
            await dataRand.DeleteUnit(unitId1);
            await dataRand.DeleteUnit(unitId2);
            await dataRand.DeleteSupplier(supplierId);
        }
    }
}
