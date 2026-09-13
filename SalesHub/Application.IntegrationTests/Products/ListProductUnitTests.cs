using Application.Features.Products.Create;
using Application.Features.Products.Units.List;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Products;

public class ListProductUnitTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public ListProductUnitTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    [Fact]
    public async Task Create_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();

        int baseUnitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int insertedId = 0;
        string internalCode = Guid.NewGuid().ToString();
        string externalCode = Guid.NewGuid().ToString();
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

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}