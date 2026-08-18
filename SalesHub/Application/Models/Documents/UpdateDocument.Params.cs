namespace Application.Models.Documents;

public class UpdateDocumentParams
{
    public Guid DocumentId {get;set;}
    public DateTime PostingDate {get;set;}
    public DateTime DocumentDate {get;set;}
    public int PeriodId {get;set;}
    public Guid UpdatedBy {get;set;}
    public string? Note {get;set;}
    public string Status {get;set;} = null!;
}
