using Microsoft.AspNetCore.Http;
using Pipeline.Infrastructure.Security;

namespace Pipeline.LocalApi.Security;

internal sealed class PerLaunchTokenAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly LocalApiTokenService _tokenService;

    public PerLaunchTokenAuthMiddleware(RequestDelegate next, LocalApiTokenService tokenService)
    {
        _next = next;
        _tokenService = tokenService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Missing Bearer launch token.");
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (!_tokenService.ValidateToken(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Invalid Bearer launch token.");
            return;
        }

        await _next(context);
    }
}
