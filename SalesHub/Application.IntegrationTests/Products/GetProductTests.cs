using Application.Exceptions;
using Application.Features.Products.Create;
using Application.Features.Products.Get;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Products;

public class GetProductTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public GetProductTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Get_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();

        int unitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int insertedId = 0;
        string internalCode = Guid.NewGuid().ToString();
        string externalCode = Guid.NewGuid().ToString();

        try
        {
            var command = new CreateProductCommand
            {
                InternalCode = internalCode,
                ExternalCode = externalCode,
                Name = $"{internalCode}-Name",
                CostingMethod = "AVG",
                BaseUnitId = unitId,
                SupplierId = supplierId,
                Active = true,
                VatRate = 1.1m
            };

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            var res = await sender.Send(new GetProductQuery { ProductId = insertedId }, CancellationToken.None);

            int testBaseUnitId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT unit_id FROM units
            WHERE unit_id = @UnitId AND name = @Name
            ", new { UnitId = command.BaseUnitId, Name = res.BaseUnitName });

            int testSupplierId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT supplier_id FROM suppliers
            WHERE supplier_id = @SupplierId AND name = @Name
            ", new { SupplierId = command.SupplierId, Name = res.SupplierName });

            Assert.Equal(insertedId, res.ProductId);
            Assert.Equal(command.InternalCode, res.InternalCode);
            Assert.Equal(command.ExternalCode, res.ExternalCode);
            Assert.Equal(command.Name, res.Name);
            Assert.Equal(command.CostingMethod, res.CostingMethod);
            Assert.Equal(command.BaseUnitId, testBaseUnitId);
            Assert.True(res.Active);
            Assert.Equal(command.SupplierId, testSupplierId);
            Assert.Equal(command.VatRate, res.VatRate);
        }
        finally
        {
            await dataRand.DeleteProductUnits(insertedId);
            await dataRand.DeleteProduct(insertedId);
            await dataRand.DeleteUnit(unitId);
            await dataRand.DeleteSupplier(supplierId);
        }
    }

    [Fact]
    public async Task Get_Should_Throw_NotFound()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
        {
           await sender.Send(new GetProductQuery { ProductId = int.MaxValue }, CancellationToken.None); 
        });

        Assert.Equal("notfound", ex.Code);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}