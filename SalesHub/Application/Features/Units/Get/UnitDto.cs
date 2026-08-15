namespace Application.Features.Units.Get;

public class UnitDto
{
    public int RowNumber { get; set; }
    public int UnitId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
