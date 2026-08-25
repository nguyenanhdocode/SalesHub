using Application.Features.Units.Create;
using Application.IntegrationTests;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.Interfaces.Units;

public class CreateUnitTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public CreateUnitTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<CreateUnitCommand, string> InvalidCommands => new()
    {
        {
            new CreateUnitCommand
            {
                Code = null!,
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateUnitCommand
            {
                Code = "",
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateUnitCommand
            {
                Code = new string('C', 51),
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateUnitCommand
            {
                Code = "Mét",
                Name = "Name"
            },
            "Code"
        },
        {
            new CreateUnitCommand
            {
                Code = "Code",
                Name = null!
            },
            "Name"
        },
        {
            new CreateUnitCommand
            {
                Code = "Code",
                Name = ""
            },
            "Name"
        },
        {
            new CreateUnitCommand
            {
                Code = "Code",
                Name = new string('C', 101),
            },
            "Name"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Create_Should_Validator_Fail(CreateUnitCommand command, string expectedProperty)
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
        int insertedId = 0;

        var command = new CreateUnitCommand
        {
            Code = code,
            Name = code
        };

        try
        {
            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT unit_id FROM units
            WHERE code = @Code AND name = @Name AND active = true;
            ", command);

            Assert.Equal(insertedId, testId);
        }
        finally
        {
            await dataRand.DeleteUnit(insertedId);
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
        int insertedId = 0;

        var command = new CreateUnitCommand
        {
            Code = code,
            Name = code
        };

        try
        {
            insertedId = await sender.Send(command, CancellationToken.None);
            Assert.NotEqual(0, insertedId);

            var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
            {
                await sender.Send(command, CancellationToken.None);
            });

            Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
        }
        finally
        {
            await dataRand.DeleteUnit(insertedId);
        }
    }
}
