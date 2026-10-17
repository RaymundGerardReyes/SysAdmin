using System.Collections.Concurrent;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Domain.Jobs;
using Pipeline.Domain.Security;

namespace Pipeline.TestKit.Fakes;

public sealed class InMemoryPrincipalAccessor : IPrincipalAccessor
{
    public Principal? Current { get; set; }
}

public sealed class InMemoryPolicyBundleProvider : IPolicyBundleProvider
{
    public PolicyBundle? Current { get; set; }
    public Task<bool> RefreshAsync(CancellationToken ct = default) => Task.FromResult(true);
}

public sealed class InMemoryAuditWriter : IAuditWriter
{
    public List<AuditEntry> Entries { get; } = new();

    public Task WriteAsync(AuditEntry entry, CancellationToken ct = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<AuditChainStatus> VerifyChainAsync(CancellationToken ct = default) =>
        Task.FromResult(new AuditChainStatus(true, null));
}

public sealed class RecordingRateLimiter : IRateLimiter
{
    public bool AllowAll { get; set; } = true;
    public List<string> RequestedKeys { get; } = new();

    public Task<bool> ShouldAllowAsync(string key, int maxRequestsPerMinute, CancellationToken ct = default)
    {
        RequestedKeys.Add(key);
        return Task.FromResult(AllowAll);
    }
}

public sealed class InMemoryApprovalService : IApprovalService
{
    private readonly ConcurrentDictionary<string, (string Fingerprint, string Requester)> _approvals = new();

    public Task<ApprovalResult> ValidateAsync(string approvalId, string operationFingerprint, string requesterUserId, CancellationToken ct = default)
    {
        if (!_approvals.TryGetValue(approvalId, out var record))
        {
            return Task.FromResult(new ApprovalResult(false, "APPROVAL_NOT_FOUND"));
        }

        if (!string.Equals(record.Fingerprint, operationFingerprint, StringComparison.Ordinal))
        {
            return Task.FromResult(new ApprovalResult(false, "FINGERPRINT_MISMATCH"));
        }

        if (!SeparationOfDutiesRule.IsApproverAllowed(record.Requester, requesterUserId))
        {
            return Task.FromResult(new ApprovalResult(false, "SOD_VIOLATION"));
        }

        return Task.FromResult(new ApprovalResult(true, "OK", "authorized_approver"));
    }

    public Task<string> RequestAsync(string operationFingerprint, string requesterUserId, CancellationToken ct = default)
    {
        var id = $"appr_{Guid.NewGuid():N}";
        _approvals[id] = (operationFingerprint, requesterUserId);
        return Task.FromResult(id);
    }

    public void SeedApproval(string approvalId, string operationFingerprint, string requesterUserId)
    {
        _approvals[approvalId] = (operationFingerprint, requesterUserId);
    }
}

public sealed class InMemoryStepUpAuthenticator : IStepUpAuthenticator
{
    public bool AlwaysFresh { get; set; } = true;

    public bool IsFresh(Principal principal, RiskTier tier, DateTimeOffset nowUtc)
    {
        if (AlwaysFresh) return true;
        if (!principal.LastStepUpAtUtc.HasValue) return false;

        var maxAge = tier switch
        {
            RiskTier.T2 => TimeSpan.FromMinutes(15),
            RiskTier.T3 => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromHours(1)
        };

        return nowUtc - principal.LastStepUpAtUtc.Value <= maxAge;
    }

    public Task<bool> ChallengeAsync(CancellationToken ct = default) => Task.FromResult(true);
}

public sealed class InMemoryCredentialStore : ICredentialStore
{
    private readonly ConcurrentDictionary<string, string> _tokens = new();

    public Task<string?> GetRefreshTokenAsync(string provider, string accountId, CancellationToken ct = default)
    {
        _tokens.TryGetValue($"{provider}:{accountId}", out var t);
        return Task.FromResult(t);
    }

    public Task SetRefreshTokenAsync(string provider, string accountId, string refreshToken, CancellationToken ct = default)
    {
        _tokens[$"{provider}:{accountId}"] = refreshToken;
        return Task.CompletedTask;
    }

    public Task RemoveRefreshTokenAsync(string provider, string accountId, CancellationToken ct = default)
    {
        _tokens.TryRemove($"{provider}:{accountId}", out _);
        return Task.CompletedTask;
    }
}
