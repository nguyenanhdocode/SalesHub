namespace Application.Features.Units.List;

public class UnitListItem
{
    public int UnitId {get;set;}
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool Active {get;set;}
    public DateTime CreatedAt {get;set;}
    public DateTime? UpdatedAt {get;set;}
}
