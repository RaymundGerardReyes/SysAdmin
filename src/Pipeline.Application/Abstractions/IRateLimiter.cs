namespace Pipeline.Application.Abstractions;

public interface IRateLimiter
{
    Task<bool> ShouldAllowAsync(string key, int maxRequestsPerMinute, CancellationToken ct = default);
}
