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

        Services = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        if (Services is IDisposable disposable)
            disposable.Dispose();
    }

    public IServiceScope CreateScope()
        => Services.CreateScope();
}
