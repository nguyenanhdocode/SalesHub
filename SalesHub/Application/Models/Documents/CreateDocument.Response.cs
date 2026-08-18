namespace Application.Models.Documents;

public class CreateDocumentResponse
{
    public Guid DocumentId {get;set;}
    public string DocumentNo {get;set;} = null!;
}
