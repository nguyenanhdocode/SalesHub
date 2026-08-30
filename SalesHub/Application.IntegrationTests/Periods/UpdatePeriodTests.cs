using System.Media;
using Application.Features.Periods.Create;
using Application.Features.Periods.Update;
using Application.Features.Suppliers.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Periods;

public class UpdatePeriodTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public UpdatePeriodTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<UpdatePeriodCommand, string> InvalidCommands => new()
    {
        {
            new UpdatePeriodCommand
            {
                Code = null!,
                Name = "Name",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30),
            },
            "Code"
        },
        {
            new UpdatePeriodCommand
            {
                Code = "",
                Name = "Name",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Code"
        },
        {
            new UpdatePeriodCommand
            {
                Code = new string('C', 51),
                Name = "Name",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Code"
        },
        {
            new UpdatePeriodCommand
            {
                Code = "Code",
                Name = null!,
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Name"
        },
        {
            new UpdatePeriodCommand
            {
                Code = "Code",
                Name = "",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Name"
        },
        {
            new UpdatePeriodCommand
            {
                Code = "Code",
                Name = new string('C', 251),
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Name"
        },
        {
            new UpdatePeriodCommand
            {
                Code = "Code",
                Name = "Name",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(-30)
            },
            "ToDate"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Update_Should_Throw_Validation_Exception(UpdatePeriodCommand command, string expectedProperty)
    {
        using var scope = _fixture.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await sender.Send(command, CancellationToken.None);
        });

        Assert.Contains(exception.Errors, x => x.PropertyName == expectedProperty);
    }

    [Fact]
    public async Task Update_Should_Success()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        string code = Guid.NewGuid().ToString("N")[..25];

        int periodId = 0;

        var createCommand = new CreatePeriodCommand
        {
            Code = code,
            Name = $"${code}name",
            FromDate = DateTime.Now,
            ToDate = DateTime.Now.AddDays(30)
        };

        try
        {
            periodId = await sender.Send(createCommand, CancellationToken.None);
            Assert.NotEqual(0, periodId);

            var updateCommand = new UpdatePeriodCommand
            {
                PeriodId = periodId,
                Code = $"{code}updated",
                Name = $"${code}name-updated",
                FromDate = DateTime.Now.AddDays(1),
                ToDate = DateTime.Now.AddDays(30).AddDays(1)
            };

            await sender.Send(updateCommand, CancellationToken.None);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT period_id FROM periods
            WHERE code = @Code AND name = @Name AND is_closed = false
            AND from_date = @FromDate AND to_date = @ToDate
            ", updateCommand);
            Assert.Equal(periodId, testId);
        }
        finally
        {
            await dataRand.DeletePeriod(periodId);
        }
    }
}