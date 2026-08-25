using System.Media;
using Application.Exceptions;
using Application.Features.Suppliers.Create;
using Application.Features.Units.Create;
using Dapper;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests.Units;

public class UpdateUnitTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public UpdateUnitTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<UpdateUnitCommand, string> InvalidCommands => new()
    {
        {
            new UpdateUnitCommand
            {
                Code = null!,
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateUnitCommand
            {
                Code = "",
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateUnitCommand
            {
                Code = new string('C', 51),
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateUnitCommand
            {
                Code = "Mét",
                Name = "Name"
            },
            "Code"
        },
        {
            new UpdateUnitCommand
            {
                Code = "Code",
                Name = null!
            },
            "Name"
        },
        {
            new UpdateUnitCommand
            {
                Code = "Code",
                Name = ""
            },
            "Name"
        },
        {
            new UpdateUnitCommand
            {
                Code = "Code",
                Name = new string('C', 101),
            },
            "Name"
        }
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task Update_Should_Throw_Validation_Exception(UpdateUnitCommand command, string expectedProperty)
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

            var updateCommand = new UpdateUnitCommand
            {
              UnitId = unitId,
              Code = $"{code}updated",
              Name = $"{code}name-updated",
              Active = false
            };

            await sender.Send(updateCommand, CancellationToken.None);

            int testId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT unit_id FROM units
            WHERE unit_id = @UnitId AND code = @Code AND name = @Name AND active = @Active
            "
            , updateCommand);

            Assert.Equal(unitId, testId);
        }
        finally
        {
            await dataRand.DeleteUnit(unitId);
        }
    }
}
