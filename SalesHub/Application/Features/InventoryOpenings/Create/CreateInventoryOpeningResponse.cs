namespace Application.Features.InventoryOpenings.Create;

public class CreateInventoryOpeningResponse
{
    public Guid DocumentId {get;set;}
    public string DocumentNo {get;set;} = null!;
}
