using System.Data;

namespace Application.Interfaces.Database;

public interface ITransactionalRequest
{
    IsolationLevel IsolationLevel {get;}
}
