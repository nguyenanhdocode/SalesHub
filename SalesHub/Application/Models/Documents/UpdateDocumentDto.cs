using System.Diagnostics.SymbolStore;
using Application.Shared;

namespace Application.Models.Documents;

public class UpdateDocumentDto
{
    public Guid DocumentId {get;set;}
    public DateTime PostingDate {get;set;}
    public DateTime DocumentDate {get;set;}
    public int PeriodId {get;set;}
    public string? Note {get;set;}
    public DocumentStatus Status {get;set;}
}
