using System.Media;
using System.Net;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Units.Delete;
using Application.Shared;
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
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        int unitId = 0;


        try
        {
            unitId = await dataRand.RandomUnit();
            Assert.True(unitId > 0);

            await sender.Send(new DeleteUnitCommand { UnitId = unitId }, CancellationToken.None);

            int count = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM units WHERE unit_id = @UnitId
            "
            , new
            {
                UnitId = unitId
            });

            Assert.Equal(0, count);
        }
        finally
        {
            await dataRand.DeleteUnit(unitId);
        }
    }
}