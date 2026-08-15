namespace Api.Models;

public sealed class ErrorResponse
{
    public bool Success => false;

    public string Code { get; init; } = "";

    public string Message { get; init; } = "";

    public object? Errors { get; init; }
}