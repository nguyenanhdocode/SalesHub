using Application.Features.Products.Create;
using Application.Features.Products.UnitConversions.Update;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Products;

public class UpdateUnitConversionsTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public UpdateUnitConversionsTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Update_Add_Should_Success()
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

            int count1 = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM unit_conversions
            WHERE src_unit_id = @SrcUnitId AND dst_unit_id = @DstUnitId AND conversion_factor = @ConversionFactor
            ", updateUnitConversionsCommand.Conversions[0]);

            Assert.Equal(1, count1);

            int count2 = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM unit_conversions
            WHERE src_unit_id = @SrcUnitId AND dst_unit_id = @DstUnitId AND conversion_factor = @ConversionFactor
            ", updateUnitConversionsCommand.Conversions[1]);

            Assert.Equal(1, count2);
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

    [Fact]
    public async Task Update_Remove_Should_Success()
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

            var updateUnitConversionsCommand1 = new UpdateUnitConversionsCommand
            {
                ProductId = insertedId,
                Conversions = new List<UnitConversionInput>
                {
                    new UnitConversionInput { SrcUnitId = baseUnitId, DstUnitId = unitId, ConversionFactor = 0.5m },
                    new UnitConversionInput { SrcUnitId = unitId, DstUnitId = baseUnitId, ConversionFactor = 0.4m }
                }
            };

            await sender.Send(updateUnitConversionsCommand1, CancellationToken.None);

            var updateUnitConversionsCommand2 = new UpdateUnitConversionsCommand
            {
                ProductId = insertedId,
                Conversions = new List<UnitConversionInput>
                {
                    new UnitConversionInput { SrcUnitId = baseUnitId, DstUnitId = unitId, ConversionFactor = 0.5m },
                }
            };

            await sender.Send(updateUnitConversionsCommand2, CancellationToken.None);

            int count1 = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM unit_conversions
            WHERE src_unit_id = @SrcUnitId AND dst_unit_id = @DstUnitId AND conversion_factor = @ConversionFactor
            ", updateUnitConversionsCommand1.Conversions[0]);

            Assert.Equal(1, count1);

            int count2 = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM unit_conversions
            WHERE src_unit_id = @SrcUnitId AND dst_unit_id = @DstUnitId AND conversion_factor = @ConversionFactor
            ", updateUnitConversionsCommand1.Conversions[1]);

            Assert.Equal(0, count2);
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