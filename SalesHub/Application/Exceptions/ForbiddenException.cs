namespace Application.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException() : base() {}

    public ForbiddenException(string code) : base(code) {}
}
