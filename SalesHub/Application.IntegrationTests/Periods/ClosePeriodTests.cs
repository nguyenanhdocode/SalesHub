using System.Media;
using Application.Features.Periods.Close;
using Application.Features.Periods.Create;
using Application.Features.Suppliers.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Periods;

public class ClosePeriodTests : IClassFixture<ApplicationFixture>, IAsyncLifetime
{
    private readonly ApplicationFixture _fixture;
    private readonly IServiceScope _scope;

    public ClosePeriodTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
    }

    [Fact]
    public async Task Close_Should_Success()
    {
        var sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = _scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = _scope.ServiceProvider.GetRequiredService<DataRandom>();
        string code = Guid.NewGuid().ToString("N");

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

            var closeCommand = new ClosePeriodCommand
            {
                PeriodId = periodId
            };

            await sender.Send(closeCommand, CancellationToken.None);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT period_id FROM periods
            WHERE code = @Code AND name = @Name AND is_closed = true
            AND from_date = @FromDate AND to_date = @ToDate
            ", command);

            Assert.Equal(periodId, testId);
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