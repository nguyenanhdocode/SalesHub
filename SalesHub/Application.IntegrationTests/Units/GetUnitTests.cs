using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Application.Features.Units.Delete;
using Application.Features.Units.Get;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Units;

public class GetUnitTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public GetUnitTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Get_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        int unitId = 0;
        string code = Guid.NewGuid().ToString("N")[..25];

        var command = new CreateUnitCommand
        {
            Code = code,
            Name = $"{code}name"
        };

        try
        {
            unitId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, unitId);

            var getCommand = new GetUnitQuery
            {
              UnitId = unitId  
            };

            var res = await sender.Send(getCommand, CancellationToken.None);
            Assert.Equal(command.Code, res.Code);
            Assert.Equal(command.Name, res.Name);
            Assert.Equal(unitId, res.UnitId);
            Assert.True(res.Active);
        }
        finally
        {
            await dataRand.DeleteUnit(unitId);
        }
    }

    [Fact]
    public async Task Get_Should_NotFound()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        int unitId = 0;
        string code = Guid.NewGuid().ToString("N")[..25];

        var command = new CreateUnitCommand
        {
            Code = code,
            Name = $"{code}name"
        };

        try
        {
            unitId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, unitId);

            var getCommand = new GetUnitQuery
            {
              UnitId = int.MaxValue  
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
               await sender.Send(getCommand, CancellationToken.None);
            });

            Assert.Equal("notfound", ex.Code);
        }
        finally
        {
            await dataRand.DeleteUnit(unitId);
        }
    }
}