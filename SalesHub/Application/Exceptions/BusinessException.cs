namespace Application.Exceptions;

public class BusinessException : Exception
{
    public string Code {get;}
    public object? Detail {get;}

    public BusinessException(string code)
    {
        Code = code;
    }

    public BusinessException(string code, object detail)
    {
        Code = code;
        Detail = detail;
    }
}
