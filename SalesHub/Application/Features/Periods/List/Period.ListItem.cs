namespace Application.Features.Periods.List;

public class PeriodListItem
{
    public int PeriodId {get;set;}
    public string Code {get;set;} = null!;
    public string Name {get;set;} = null!;
    public DateTime FromDate {get;set;}
    public DateTime ToDate {get;set;}
    public bool IsClosed {get;set;}
}