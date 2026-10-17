using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Pipeline.Infrastructure.Security;
using Pipeline.LocalApi.Security;

var builder = WebApplication.CreateBuilder(args);

// Hardened LocalApi services
builder.Services.AddSingleton(new LocalApiTokenService());

var app = builder.Build();

app.UseMiddleware<LoopbackOnlyMiddleware>();
app.UseMiddleware<HostHeaderMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<PerLaunchTokenAuthMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

app.Run();

// For WebApplicationFactory in tests
public partial class Program { }
