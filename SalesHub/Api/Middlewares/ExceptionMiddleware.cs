using System.Text.Json;
using FluentValidation;
using Api.Models;
using Application.Exceptions;
using Npgsql;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            await HandleException(context, ex);
        }
    }

    private static async Task HandleException(
        HttpContext context,
        Exception exception)
    {
        var response = new ErrorResponse();

        switch (exception)
        {
            case ValidationException ex:

                context.Response.StatusCode = 400;

                response = new ErrorResponse
                {
                    Code = "validation_error",
                    Message = "Validation error",
                    Errors = ex.Errors
                };

                break;
            case BusinessException ex:
                context.Response.StatusCode = 422;
                response = new ErrorResponse
                {
                    Code = ex.Message,
                    Message = ex.Message,
                    Errors = ex.Detail
                };
                break;
            case AuthenticateException ex:
                context.Response.StatusCode = 401;
                response = new ErrorResponse
                {
                    Code = "unauthorized",
                    Message = ex.Message
                };
                break;
            case ForbiddenException ex:
                context.Response.StatusCode = 403;
                response = new ErrorResponse
                {
                    Code = "forbidden",
                    Message = ex.Message
                };
                break;
            case PostgresException ex when (ex.SqlState == PostgresErrorCodes.UniqueViolation):
                context.Response.StatusCode = 422;
                response = new ErrorResponse
                {
                    Code = "exists",
                    Message = "Exists"
                };
                break;
            case PostgresException ex when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation):
                context.Response.StatusCode = 422;
                response = new ErrorResponse
                {
                    Code = "ref_records_violation",
                    Message = "Ref records violation"
                };
                break;
            default:
                context.Response.StatusCode = 500;
                response = new ErrorResponse
                {
                    Code = "internal_server_error",
                    Message = exception.Message
                };

                break;
        }

        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}