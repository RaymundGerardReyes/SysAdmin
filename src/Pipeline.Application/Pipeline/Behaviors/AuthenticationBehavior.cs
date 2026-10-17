using Pipeline.Application.Abstractions;
using Pipeline.Contracts.Errors;
using Pipeline.Domain.Common;

namespace Pipeline.Application.Pipeline.Behaviors;

public sealed class AuthenticationBehavior
{
    private readonly IPrincipalAccessor _principalAccessor;
    private readonly IClock _clock;

    public AuthenticationBehavior(IPrincipalAccessor principalAccessor, IClock clock)
    {
        _principalAccessor = principalAccessor;
        _clock = clock;
    }

    public Result<TResponse>? Check<TResponse>()
    {
        var principal = _principalAccessor.Current;
        if (principal is null)
        {
            return Result<TResponse>.Failure(ErrorCodes.AuthnMissing, "User is not authenticated.", "AUTHZ_P01");
        }

        if (principal.SessionExpiresAtUtc <= _clock.UtcNow)
        {
            return Result<TResponse>.Failure(ErrorCodes.SessionInvalid, "Session has expired.", "AUTHZ_P02");
        }

        return null;
    }
}
