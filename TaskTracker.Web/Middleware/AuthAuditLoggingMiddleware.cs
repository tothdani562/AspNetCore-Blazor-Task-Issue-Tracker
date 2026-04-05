using System.Diagnostics;
using System.Security.Claims;

namespace TaskTracker.Web.Middleware;

public class AuthAuditLoggingMiddleware
{
    private static readonly PathString AuthPathPrefix = new("/auth");

    private readonly RequestDelegate _next;
    private readonly ILogger<AuthAuditLoggingMiddleware> _logger;

    public AuthAuditLoggingMiddleware(RequestDelegate next, ILogger<AuthAuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments(AuthPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        var request = context.Request;
        var response = context.Response;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (response.StatusCode >= StatusCodes.Status400BadRequest)
        {
            _logger.LogWarning(
                "Auth endpoint request completed with error. Method={Method}, Path={Path}, StatusCode={StatusCode}, DurationMs={DurationMs}, UserId={UserId}, RemoteIp={RemoteIp}",
                request.Method,
                request.Path,
                response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                userId,
                remoteIp);
            return;
        }

        _logger.LogInformation(
            "Auth endpoint request completed. Method={Method}, Path={Path}, StatusCode={StatusCode}, DurationMs={DurationMs}, UserId={UserId}, RemoteIp={RemoteIp}",
            request.Method,
            request.Path,
            response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            userId,
            remoteIp);
    }
}

public static class AuthAuditLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthAuditLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthAuditLoggingMiddleware>();
    }
}
