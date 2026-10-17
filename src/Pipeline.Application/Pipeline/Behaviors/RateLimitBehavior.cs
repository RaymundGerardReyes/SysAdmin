using Pipeline.Application.Abstractions;
using Pipeline.Contracts.Errors;
using Pipeline.Domain.Common;

namespace Pipeline.Application.Pipeline.Behaviors;

public sealed class RateLimitBehavior
{
    private readonly IRateLimiter _rateLimiter;
    private readonly IPrincipalAccessor _principalAccessor;

    public RateLimitBehavior(IRateLimiter rateLimiter, IPrincipalAccessor principalAccessor)
    {
        _rateLimiter = rateLimiter;
        _principalAccessor = principalAccessor;
    }

    public async Task<Result<TResponse>?> CheckRateLimitAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default)
        where TRequest : IUseCaseRequest
    {
        var principal = _principalAccessor.Current;
        var key = $"{principal?.UserId ?? "ANON"}:{request.OperationName}";

        var allowed = await _rateLimiter.ShouldAllowAsync(key, 60, ct).ConfigureAwait(false);
        if (!allowed)
        {
            return Result<TResponse>.Failure(ErrorCodes.RateLimited, "Rate limit exceeded.", "AUTHZ_P09");
        }

        return null;
    }
}
