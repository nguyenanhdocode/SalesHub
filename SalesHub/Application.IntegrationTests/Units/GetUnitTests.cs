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

public class GetUnitTests : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public GetUnitTests(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Get_Should_Success()
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
            var insertedId = await sender.Send(command, CancellationToken.None);
            Assert.True(insertedId > 0);

            var res = await sender.Send(new GetUnitQuery
            {
                UnitId = insertedId
            }, CancellationToken.None);

            Assert.Equal(code, res.Code);
            Assert.Equal(command.Name, res.Name);
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
    public async Task Get_Should_Throw_NotFound()
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
            var insertedId = await sender.Send(command, CancellationToken.None);
            Assert.True(insertedId > 0);

            var ex = await Assert.ThrowsAsync<BusinessException>(async () =>
            {
                await sender.Send(new GetUnitQuery
                {
                    UnitId = int.MaxValue
                }, CancellationToken.None);
            });

            Assert.Equal("notfound", ex.Code);
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
