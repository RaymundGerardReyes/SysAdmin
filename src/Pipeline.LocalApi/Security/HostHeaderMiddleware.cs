using Microsoft.AspNetCore.Http;

namespace Pipeline.LocalApi.Security;

public sealed class HostHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public HostHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (!string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Bad Request: Invalid Host header (DNS rebinding protection).");
            return;
        }

        await _next(context);
    }
}
