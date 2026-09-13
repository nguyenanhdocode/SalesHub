using System.Media;
using Application.Features.Periods.Create;
using Application.Features.Periods.Delete;
using Application.Features.Suppliers.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Periods;

public class DeletePeriodTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public DeletePeriodTests(ApplicationFixture fixture)
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
        string code = Guid.NewGuid().ToString();

        int periodId = 0;

        var command = new CreatePeriodCommand
        {
          Code = code,
          Name = $"${code}name",
          FromDate = DateTime.Now,
          ToDate = DateTime.Now.AddDays(30)
        };

        try
        {
            periodId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, periodId);
            
            await sender.Send(new DeletePeriodCommand
            {
               PeriodId = periodId 
            });

            int count = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(1) FROM periods WHERE period_id = @PeriodId;
            ", new { PeriodId = periodId });

            Assert.Equal(0, count);
        }
        finally
        {
            await dataRand.DeletePeriod(periodId);
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