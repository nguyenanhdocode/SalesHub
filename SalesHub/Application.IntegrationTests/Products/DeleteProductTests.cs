using Application.Features.Products.Create;
using Application.Features.Products.Delete;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Products;

public class DeleteProductTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public DeleteProductTests(ApplicationFixture fixture)
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

        int unitId = await dataRand.RandomUnit();
        int supplierId = await dataRand.RandomSupplier();
        int insertedId = 0;
        string internalCode = Guid.NewGuid().ToString("N")[..20];
        string externalCode = Guid.NewGuid().ToString("N")[..20];

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
                Active = true
            };

            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            await sender.Send(new DeleteProductCommand { ProductId = insertedId }, CancellationToken.None);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM products
            WHERE product_id = @ProductId
            ", new { ProductId = insertedId });

            Assert.Equal(0, testId);
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