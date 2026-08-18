using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Branchs.Create;

public class CreateBranchCommand : IRequest<int>, ITransactionalRequest
{
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public string? Address {get;set;}
    public string? Phone {get;set;}
    public string? Email {get;set;}
    public string? TaxCode {get;set;}

    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
