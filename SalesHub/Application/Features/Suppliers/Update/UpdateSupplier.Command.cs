using System.Data;
using Application.Interfaces.Database;
using MediatR;

namespace Application.Features.Suppliers.Update;

public class UpdateSupplierCommand : IRequest<int>, ITransactionalRequest
{
    public int SupplierId {get;set;}
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public string? ContactPerson {get;set;}
    public string? Phone {get;set;}
    public string? TaxCode {get;set;}
    public string? Email {get;set;}
    public string? Address {get;set;}
    IsolationLevel ITransactionalRequest.IsolationLevel => IsolationLevel.ReadCommitted;
}
