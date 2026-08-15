namespace Application.Models.Common;

public class PagedResult<T> where T: class
{
    public PagedResult(IEnumerable<T> rows, int totalPage, int pageNumer, int pageSize)
    {
        Rows = rows;
        TotalPage = totalPage;
        PageNumer = pageNumer;
        PageSize = pageSize;
    }

    public IEnumerable<T> Rows {get; private set;}
    public int TotalPage {get;set;}
    public int PageNumer {get;set;}
    public int PageSize {get;set;}
}
