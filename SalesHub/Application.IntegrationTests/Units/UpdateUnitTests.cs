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
    public async Task Update_Should_Validator_Fail(UpdateUnitCommand command, string expectedProperty)
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
        var code = Guid.NewGuid().ToString("N")[..25];

        var command = new CreateUnitCommand
        {
            Code = code,
            Name = $"Đơn vị tính {code}"
        };

        var updateCode = $"{code}-edited";

        try
        {
            var insertedId = await sender.Send(command, CancellationToken.None);
            Assert.True(insertedId > 0);

            var updateCommand = new UpdateUnitCommand
            {
                UnitId = insertedId,
                Code = updateCode,
                Name = "Đơn vị tính edited",
                Active = false
            };

            await sender.Send(updateCommand, CancellationToken.None);

            int updateId = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT unit_id FROM units
            WHERE code = @Code AND name = @Name AND active = @Active;
            ", updateCommand);

            Assert.Equal(insertedId, updateId);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"DELETE FROM units WHERE code = ANY(@Codes)", new
            {
               Codes = new string[] {code, updateCode}
            });
        }
    }

    [Fact]
    public async Task Update_Should_Throw_Exists()
    {
        using var scope = _fixture.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var code1 = Guid.NewGuid().ToString("N")[..25];
        var command1 = new CreateUnitCommand
        {
            Code = code1,
            Name = $"Đơn vị tính {code1}"
        };

        var code2 = Guid.NewGuid().ToString("N")[..25];
        var command2 = new CreateUnitCommand
        {
            Code = code2,
            Name = $"Đơn vị tính {code2}"
        };

        var updateCode = code2;

        try
        {
            var insertedId1 = await sender.Send(command1, CancellationToken.None);
            Assert.True(insertedId1 > 0);

            var insertedId2 = await sender.Send(command2, CancellationToken.None);
            Assert.True(insertedId2 > 0);

            var updateCommand = new UpdateUnitCommand
            {
                UnitId = insertedId1,
                Code = code2,
                Name = "Đơn vị tính edited",
                Active = false
            };

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
               await sender.Send(updateCommand, CancellationToken.None); 
            });

            Assert.Equal("exists", ex.Code);

        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"DELETE FROM units WHERE code = ANY(@Codes)", new
            {
               Codes = new string[] {code1, code2}
            });
        }
    }
}