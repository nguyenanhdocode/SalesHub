using Application.Features.Products.Create;
using Application.Features.Products.Units.List;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Products;

public class ListProductUnitTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public ListProductUnitTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
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
                UnitIds = new List<int> { unitId, baseUnitId }
            };

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            var res = await sender.Send(new ListProductUnitQuery { ProductId = insertedId }, CancellationToken.None);

            Assert.Contains(baseUnitId, res.Select(p => p.UnitId));
            Assert.Contains(unitId, res.Select(p => p.UnitId));
            Assert.True(res.Single(p => p.UnitId == baseUnitId).IsBaseUnit);
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
}