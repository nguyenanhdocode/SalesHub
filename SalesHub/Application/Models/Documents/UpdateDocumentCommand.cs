using System.Diagnostics.SymbolStore;
using Application.Interfaces.Common;
using Application.Shared;
using MediatR;

namespace Application.Models.Documents;

public class UpdateDocumentCommand : IRequest
{
    public Guid DocumentId {get;set;}
    public DateTime PostingDate {get;set;}
    public DateTime DocumentDate {get;set;}
    public int PeriodId {get;set;}
    public string? Note {get;set;}
    public DocumentStatus Status {get;set;}
}
