namespace Application.Models.Documents;

public class RefDocumentResponse
{
    public Guid DocumentId {get;set;}
    public string DocumentNo {get;set;} = null!;
    public string DocumentType {get;set;} = null!;
}
