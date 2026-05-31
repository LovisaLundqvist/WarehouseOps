using System.Net;

namespace WarehouseOps.Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
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
        catch (BadHttpRequestException exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                HttpStatusCode.BadRequest,
                "Invalid request",
                "The request could not be processed.");
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(
                context,
                exception,
                HttpStatusCode.InternalServerError,
                "Unexpected error",
                "Something went wrong. Try again later or contact support.");
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        HttpStatusCode statusCode,
        string title,
        string message)
    {
        var traceId = context.TraceIdentifier;

        _logger.LogError(
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}",
            traceId);

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            title,
            message,
            statusCode = (int)statusCode,
            traceId
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
