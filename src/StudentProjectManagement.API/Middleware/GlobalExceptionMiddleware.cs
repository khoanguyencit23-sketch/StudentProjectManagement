using System.Text.Json;
using StudentProjectManagement.Application.Common;

namespace StudentProjectManagement.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(exception, "An exception occurred after the response started.");
                throw;
            }

            var (statusCode, code, message) = MapException(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
            }
            else
            {
                _logger.LogWarning(exception, "Request rejected. TraceId: {TraceId}", context.TraceIdentifier);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = ApiResponse<object?>.FailureResponse(
                code,
                message,
                new ApiError(context.TraceIdentifier));

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                context.RequestAborted);
        }
    }

    private static (int StatusCode, string Code, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "BAD_REQUEST", exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED", exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND", exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, "CONFLICT", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_SERVER_ERROR",
                "An unexpected error occurred. Contact the administrator with the trace ID.")
        };
    }
}
