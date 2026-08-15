namespace Application.Database;
using Npgsql;

public class DbSession
{
    public NpgsqlConnection Connection {get;internal set;} = null!;
    public NpgsqlTransaction Transaction {get;internal set;} = null!;
}
