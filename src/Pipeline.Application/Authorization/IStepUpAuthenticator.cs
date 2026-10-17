using Pipeline.Domain.Security;

namespace Pipeline.Application.Authorization;

public interface IStepUpAuthenticator
{
    bool IsFresh(Principal principal, RiskTier tier, DateTimeOffset nowUtc);
    Task<bool> ChallengeAsync(CancellationToken ct = default);
}
