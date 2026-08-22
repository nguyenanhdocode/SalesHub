using System.Data;
using Application.Database;
using Application.Features.Test;
using Application.Interfaces.Database;
using Dapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application.IntegrationTests;

public class DatabaseTest : IClassFixture<ApplicationFixture>
{
    private readonly ApplicationFixture _fixture;

    public DatabaseTest(ApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Select_Should_Success()
    {
        using var scope = _fixture.CreateScope();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        int res = await sender.Send(new TestDatabaseCommand(), CancellationToken.None);

        Assert.True(res == 1);
    }
}
