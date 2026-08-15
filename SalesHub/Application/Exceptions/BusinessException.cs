namespace Application.Exceptions;

public class BusinessException : Exception
{
    public BusinessException() : base() {}

    public BusinessException(string code) : base(code) {}
}
