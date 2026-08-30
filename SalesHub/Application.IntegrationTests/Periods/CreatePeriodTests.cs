using System.Media;
using Application.Features.Periods.Create;
using Application.Features.Suppliers.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Periods;

public class CreatePeriodTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public CreatePeriodTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<CreatePeriodCommand, string> InvalidCommands => new()
    {
        {
            new CreatePeriodCommand
            {
                Code = null!,
                Name = "Name",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Code"
        },
        {
            new CreatePeriodCommand
            {
                Code = "",
                Name = "Name",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Code"
        },
        {
            new CreatePeriodCommand
            {
                Code = new string('C', 51),
                Name = "Name",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Code"
        },
        {
            new CreatePeriodCommand
            {
                Code = "Code",
                Name = null!,
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Name"
        },
        {
            new CreatePeriodCommand
            {
                Code = "Code",
                Name = "",
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Name"
        },
        {
            new CreatePeriodCommand
            {
                Code = "Code",
                Name = new string('C', 251),
                FromDate = DateTime.Now,
                ToDate = DateTime.Now.AddDays(30)
            },
            "Name"
        },
        {
            new CreatePeriodCommand
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
    public async Task Create_Should_Throw_Validation_Exception(CreatePeriodCommand command, string expectedProperty)
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
    public async Task Create_Should_Success()
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
          FromDate = DateTime.Now,
          ToDate = DateTime.Now.AddDays(30)
        };

        try
        {
            periodId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, periodId);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT period_id FROM periods
            WHERE code = @Code AND name = @Name AND is_closed = false
            AND from_date = @FromDate AND to_date = @ToDate
            ", command);
            Assert.Equal(periodId, testId);
        }
        finally
        {
            await dataRand.DeletePeriod(periodId);
        }
    }

    [Fact]
    public async Task Create_Should_Throw_Unique_Violation()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();
        var dataRand = scope.ServiceProvider.GetRequiredService<DataRandom>();
        string code = Guid.NewGuid().ToString("N")[..25];

        int periodId1 = 0, periodId2 = 0;

        var command = new CreatePeriodCommand
        {
          Code = code,
          Name = $"${code}name",
          FromDate = DateTime.Now,
          ToDate = DateTime.Now.AddDays(30)
        };

        try
        {
            periodId1 = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, periodId1);

            var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
               periodId2 = await sender.Send(command, CancellationToken.None);
            });

            Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
            Assert.Equal("periods_code_key", ex.ConstraintName);
        }
        finally
        {
            await dataRand.DeletePeriod(periodId1);
            await dataRand.DeletePeriod(periodId2);
        }
    }
}
