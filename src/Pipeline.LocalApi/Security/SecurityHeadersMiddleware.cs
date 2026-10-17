using Microsoft.AspNetCore.Http;

namespace Pipeline.LocalApi.Security;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.Append("Cache-Control", "no-store");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        // Strictly no Access-Control-Allow-Origin headers

        await _next(context);
    }
}
