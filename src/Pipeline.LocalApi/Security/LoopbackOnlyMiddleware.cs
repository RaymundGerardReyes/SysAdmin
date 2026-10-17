using System.Net;
using Microsoft.AspNetCore.Http;

namespace Pipeline.LocalApi.Security;

public sealed class LoopbackOnlyMiddleware
{
    private readonly RequestDelegate _next;

    public LoopbackOnlyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is not null && !IPAddress.IsLoopback(remoteIp))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden: Remote connections not allowed.");
            return;
        }

        await _next(context);
    }
}
