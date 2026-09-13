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

public class GetUnitTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public GetUnitTests(ApplicationFixture fixture)
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
    public async Task Get_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        int unitId = 0;
        string code = Guid.NewGuid().ToString();

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
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        int unitId = 0;
        string code = Guid.NewGuid().ToString();

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