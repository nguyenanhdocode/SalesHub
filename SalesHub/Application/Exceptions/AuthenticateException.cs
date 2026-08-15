namespace Application.Exceptions;

public class AuthenticateException : Exception
{
    public AuthenticateException() : base() {}

    public AuthenticateException(string message) : base(message) {}
}
