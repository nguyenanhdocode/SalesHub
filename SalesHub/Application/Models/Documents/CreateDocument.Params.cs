namespace Application.Models.Documents;

public class CreateDocumentParams
{
    public Guid DocumentId {get;set;}
    public string DocumentNo {get;set;} = null!;
    public DateTime PostingDate {get;set;}
    public DateTime DocumentDate {get;set;}
    public int PeriodId {get;set;}
    public string DocumentType {get;set;} = null!;
    public Guid CreatedBy {get;set;}
    public string? Note {get;set;}
    public string Status {get;set;} = null!;
}
