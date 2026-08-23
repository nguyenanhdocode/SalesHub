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
        var code = Guid.NewGuid().ToString("N")[..25];

        var command = new CreateUnitCommand
        {
            Code = code,
            Name = $"Đơn vị tính {code}"
        };

        try
        {
            var res = await sender.Send(command, CancellationToken.None);
            Assert.True(res > 0);

            int id = await dbSession.Connection.ExecuteScalarAsync<int>(@"
            SELECT unit_id FROM units
            WHERE code = @Code AND name = @Name AND active = true;
            ", command);

            Assert.Equal(id, res);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"DELETE FROM units WHERE code = @Code", new
            {
               Code = code
            });
        }
    }

    [Fact]
    public async Task Create_Should_Throw_Exists()
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

        try
        {
            var res = await sender.Send(command, CancellationToken.None);
            Assert.True(res > 0);

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
               await sender.Send(command, CancellationToken.None);  
            });

            Assert.Equal("exists", ex.Code);
        }
        finally
        {
            await dbSession.Connection.ExecuteAsync(@"DELETE FROM units WHERE code = @Code", new
            {
               Code = code
            });
        }
    }
}