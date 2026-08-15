using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Branchs.Create;

public class CreateBranchCommand : IRequest<CreateBranchResponse>, ITransactionalRequest
{
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public string? Address {get;set;}
    public string? Phone {get;set;}
    public string? Email {get;set;}
    public string? TaxCode {get;set;}

    public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
}
