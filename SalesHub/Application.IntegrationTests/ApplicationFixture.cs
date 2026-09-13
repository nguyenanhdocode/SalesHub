using Application.Interfaces.Security;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Application.IntegrationTests;

public class ApplicationFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; } = default!;

    public IConfiguration Configuration { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Configuration);
        services.AddApplication(Configuration);
        services.AddScoped<DbSession>(_ => new DbSession(Configuration));
        services.AddScoped<DataRandom>();
        services.AddScoped<MockCurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<MockCurrentUser>());

        Services = services.BuildServiceProvider();
    }

    public virtual async Task DisposeAsync()
    {
        if (Services is IDisposable disposable)
            disposable.Dispose();
    }

    public IServiceScope CreateScope()
    {
        var scope = Services.CreateScope();
        var mockCurrentUser = scope.ServiceProvider.GetRequiredService<MockCurrentUser>();
        var dbSession = scope.ServiceProvider.GetRequiredService<DbSession>();

        var userId = dbSession.Connection.ExecuteScalar<Guid>(@"
            INSERT INTO users (username, password)
            VALUES (@Username, @Password)
            RETURNING user_id;
        ", new
        {
            Username = $"integration-{Guid.NewGuid():N}",
            Password = Guid.NewGuid().ToString("N")
        });

        mockCurrentUser.UserId = userId;

        return new CleanupScope(scope, dbSession.Connection, userId);
    }

    private sealed class CleanupScope : IServiceScope
    {
        private readonly IServiceScope _innerScope;
        private readonly NpgsqlConnection _connection;
        private readonly Guid _userId;

        public CleanupScope(IServiceScope innerScope, NpgsqlConnection connection, Guid userId)
        {
            _innerScope = innerScope;
            _connection = connection;
            _userId = userId;
        }

        public IServiceProvider ServiceProvider => _innerScope.ServiceProvider;

        public void Dispose()
        {
            try
            {
                _connection.Execute(@"
                    DELETE FROM users WHERE user_id = @UserId;
                ", new { UserId = _userId });
            }
            finally
            {
                _innerScope.Dispose();
            }
        }
    }
}
