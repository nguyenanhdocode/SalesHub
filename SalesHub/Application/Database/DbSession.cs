namespace Application.Database;
using Npgsql;

public class DbSession
{
    public NpgsqlConnection Connection {get; set;} = null!;
    public NpgsqlTransaction Transaction {get; set;} = null!;
}
