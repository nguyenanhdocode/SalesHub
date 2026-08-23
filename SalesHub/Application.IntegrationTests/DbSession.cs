using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Application.IntegrationTests;

public class DbSession : IDisposable
{
    private readonly IConfiguration _configuration;
    public DbSession(IConfiguration configuration)
    {
        _configuration = configuration;

        Connection = new NpgsqlConnection(_configuration.GetConnectionString("Default"));
        Connection.Open();
    }

    public NpgsqlConnection Connection {get;}

    public void Dispose()
    {
        Connection.Close();
        Connection.Dispose();
    }
}
