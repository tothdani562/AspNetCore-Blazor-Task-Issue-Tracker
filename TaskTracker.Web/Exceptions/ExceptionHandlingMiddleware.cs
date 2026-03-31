using System.Text.Json;
using TaskTracker.Web.Dtos;

namespace TaskTracker.Web.Exceptions;

/// <summary>
/// Globális exception handling middleware az API hibák egységes kezeléséhez
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Path = context.Request.Path,
            Timestamp = DateTime.UtcNow,
            Message = exception.Message,
            StatusCode = context.Response.StatusCode
        };

        // Specifikus exception típusokhoz logika később egészíthető ki
        switch (exception)
        {
            case ArgumentException:
                response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                break;
            case ConflictException:
                response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                break;
            case UnauthorizedAccessException:
                response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                break;
            default:
                response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = "Az kérés feldolgozása során hiba történt.";
                break;
        }

        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Extension metódusok az ExceptionHandlingMiddleware regisztrálásához
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
