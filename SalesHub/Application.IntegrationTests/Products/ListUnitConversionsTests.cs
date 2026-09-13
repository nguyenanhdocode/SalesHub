using Application.Features.Products.Create;
using Application.Features.Products.UnitConversions.List;
using Application.Features.Products.UnitConversions.Update;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Products;

public class ListUnitConversionsTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public ListUnitConversionsTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
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

    [Fact]
    public async Task List_Should_Success()
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

            var updateUnitConversionsCommand = new UpdateUnitConversionsCommand
            {
                ProductId = insertedId,
                Conversions = new List<UnitConversionInput>
                {
                    new UnitConversionInput { SrcUnitId = baseUnitId, DstUnitId = unitId, ConversionFactor = 0.5m },
                    new UnitConversionInput { SrcUnitId = unitId, DstUnitId = baseUnitId, ConversionFactor = 0.4m }
                }
            };

            await sender.Send(updateUnitConversionsCommand, CancellationToken.None);

            var res = await sender.Send(new ListUnitConversionsQuery { ProductId = insertedId }, CancellationToken.None);

            Assert.Contains(res, p => p.SrcUnitId == baseUnitId && p.DstUnitId == unitId && p.ConversionFactor == 0.5m);
            Assert.Contains(res, p => p.SrcUnitId == unitId && p.DstUnitId == baseUnitId && p.ConversionFactor == 0.4m);
        }
        finally
        {
            await dataRand.DeleteUnitConversions(insertedId);
            await dataRand.DeleteProductUnits(insertedId);
            await dataRand.DeleteProduct(insertedId);
            await dataRand.DeleteUnit(baseUnitId);
            await dataRand.DeleteUnit(unitId);
            await dataRand.DeleteSupplier(supplierId);
        }
    }
}