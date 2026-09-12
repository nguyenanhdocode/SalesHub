using System.Data;
using Application.Database;
using Application.Features.Test;
using Application.Interfaces.Database;
using Application.Interfaces.Security;
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

    [Fact]
    public async Task Scope_Should_Create_And_Remove_Current_User()
    {
        Guid userId;
        using (var scope = _fixture.CreateScope())
        {
            var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();
            var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

            userId = currentUser.UserId;

            var exists = await dbSession.Connection.ExecuteScalarAsync<bool>(@"
                SELECT EXISTS (
                    SELECT 1 FROM users WHERE user_id = @UserId
                );
            ", new { UserId = userId });

            Assert.True(exists);
        }

        using var verificationSession = new DbSession(_fixture.Configuration);
        var deleted = await verificationSession.Connection.ExecuteScalarAsync<bool>(@"
            SELECT EXISTS (
                SELECT 1 FROM users WHERE user_id = @UserId
            );
        ", new { UserId = userId });

        Assert.False(deleted);
    }
}
