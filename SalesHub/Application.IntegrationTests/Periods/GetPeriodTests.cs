using System.Media;
using Application.Features.Periods.Create;
using Application.Features.Periods.Get;
using Application.Features.Suppliers.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Periods;

public class GetPeriodTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public GetPeriodTests(ApplicationFixture fixture)
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
        string code = Guid.NewGuid().ToString("N")[..25];

        int periodId = 0;

        var command = new CreatePeriodCommand
        {
          Code = code,
          Name = $"${code}name",
          FromDate = DateTime.UtcNow,
          ToDate = DateTime.UtcNow.AddDays(30)
        };

        try
        {
            periodId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, periodId);

            var res = await sender.Send(new GetPeriodQuery { PeriodId = periodId }, CancellationToken.None);

            Assert.Equal(periodId, res.PeriodId);
            Assert.Equal(command.Code, res.Code);
            Assert.Equal(command.Name, res.Name);
            Assert.Equal(command.FromDate.Year, res.FromDate.Year);
            Assert.Equal(command.FromDate.Month, res.FromDate.Month);
            Assert.Equal(command.FromDate.Day, res.FromDate.Day);
            Assert.Equal(command.FromDate.Hour, res.FromDate.Hour);
            Assert.Equal(command.FromDate.Minute, res.FromDate.Minute);

            Assert.Equal(command.ToDate.Year, res.ToDate.Year);
            Assert.Equal(command.ToDate.Month, res.ToDate.Month);
            Assert.Equal(command.ToDate.Day, res.ToDate.Day);
            Assert.Equal(command.ToDate.Hour, res.ToDate.Hour);
            Assert.Equal(command.ToDate.Minute, res.ToDate.Minute);
            Assert.False(res.IsClosed);
        }
        finally
        {
            await dataRand.DeletePeriod(periodId);
        }
    }
}