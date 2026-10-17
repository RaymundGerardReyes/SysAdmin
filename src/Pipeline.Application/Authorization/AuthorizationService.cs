using Pipeline.Application.Abstractions;
using Pipeline.Contracts.Errors;
using Pipeline.Domain.Security;

namespace Pipeline.Application.Authorization;

public sealed class AuthorizationService : IAuthorizationService
{
    private readonly IPrincipalAccessor _principalAccessor;
    private readonly IPolicyBundleProvider _policyBundleProvider;
    private readonly IResourceAuthorizer _resourceAuthorizer;
    private readonly IStepUpAuthenticator _stepUpAuthenticator;
    private readonly IApprovalService _approvalService;
    private readonly IRateLimiter _rateLimiter;
    private readonly IClock _clock;
    private readonly IAuditWriter _auditWriter;

    public AuthorizationService(
        IPrincipalAccessor principalAccessor,
        IPolicyBundleProvider policyBundleProvider,
        IResourceAuthorizer resourceAuthorizer,
        IStepUpAuthenticator stepUpAuthenticator,
        IApprovalService approvalService,
        IRateLimiter rateLimiter,
        IClock clock,
        IAuditWriter auditWriter)
    {
        _principalAccessor = principalAccessor;
        _policyBundleProvider = policyBundleProvider;
        _resourceAuthorizer = resourceAuthorizer;
        _stepUpAuthenticator = stepUpAuthenticator;
        _approvalService = approvalService;
        _rateLimiter = rateLimiter;
        _clock = clock;
        _auditWriter = auditWriter;
    }

    public async Task<AuthorizationDecision> AuthorizeAsync(AuthorizationRequest request, CancellationToken ct = default)
    {
        var decision = AuthorizationDecision.Deny(ErrorCodes.InternalDeny, "AUTHZ_PX");
        var now = _clock.UtcNow;
        var principal = _principalAccessor.Current;

        try
        {
            // S01: Principal present and authenticated?
            if (principal is null)
            {
                decision = AuthorizationDecision.Deny(ErrorCodes.AuthnMissing, "AUTHZ_P01");
                return decision;
            }

            // S02: Session not expired / revoked?
            if (principal.SessionExpiresAtUtc <= now)
            {
                decision = AuthorizationDecision.Deny(ErrorCodes.SessionInvalid, "AUTHZ_P02");
                return decision;
            }

            // S03: Policy bundle valid and not expired?
            var bundle = _policyBundleProvider.Current;
            if (bundle is null || bundle.ExpiresAtUtc <= now)
            {
                decision = AuthorizationDecision.Deny(ErrorCodes.PolicyInvalid, "AUTHZ_P03");
                return decision;
            }

            // S04: Operation declares a required permission in catalog?
            if (string.IsNullOrWhiteSpace(request.RequiredPermission))
            {
                decision = AuthorizationDecision.Deny(ErrorCodes.OperationUnmapped, "AUTHZ_P04");
                return decision;
            }

            Permission permission;
            try
            {
                permission = new Permission(request.RequiredPermission);
            }
            catch
            {
                decision = AuthorizationDecision.Deny(ErrorCodes.OperationUnmapped, "AUTHZ_P04");
                return decision;
            }

            if (!PermissionCatalog.Exists(permission))
            {
                decision = AuthorizationDecision.Deny(ErrorCodes.OperationUnmapped, "AUTHZ_P04");
                return decision;
            }

            // S05: Principal holds permission via any role?
            if (!principal.Has(permission))
            {
                decision = AuthorizationDecision.Deny(ErrorCodes.PermissionMissing, "AUTHZ_P05");
                return decision;
            }

            // S06: Resource within principal's scope?
            if (!_resourceAuthorizer.IsInScope(principal, request.ResourceIds))
            {
                decision = AuthorizationDecision.Deny(ErrorCodes.ResourceOutOfScope, "AUTHZ_P06");
                return decision;
            }

            var tier = PermissionCatalog.All[permission];

            // S07: Risk tier >= T2: step-up fresh?
            if (tier is RiskTier.T2 or RiskTier.T3)
            {
                if (!_stepUpAuthenticator.IsFresh(principal, tier, now))
                {
                    decision = AuthorizationDecision.Challenge(ErrorCodes.StepupRequired, "AUTHZ_P07");
                    return decision;
                }
            }

            // S08: Risk tier == T3: approval present and valid (SoD)?
            if (tier == RiskTier.T3)
            {
                // Inherent Separation of Duties check: cannot manage own roles
                if (string.Equals(permission.Value, "admin.users.manage", StringComparison.Ordinal) &&
                    request.ResourceIds.TryGetValue("targetUserId", out var targetUser) &&
                    SeparationOfDutiesRule.IsSelfModificationAttempt(principal.UserId, targetUser))
                {
                    decision = AuthorizationDecision.Deny(ErrorCodes.SodViolation, "AUTHZ_P08b");
                    return decision;
                }

                if (string.IsNullOrWhiteSpace(request.ApprovalId))
                {
                    decision = AuthorizationDecision.PendingApproval(ErrorCodes.ApprovalRequired, "AUTHZ_P08a");
                    return decision;
                }

                var approval = await _approvalService.ValidateAsync(
                    request.ApprovalId,
                    request.OperationFingerprint,
                    principal.UserId,
                    ct).ConfigureAwait(false);

                if (!approval.IsValid)
                {
                    decision = AuthorizationDecision.Deny(approval.ReasonCode, "AUTHZ_P08b");
                    return decision;
                }
            }

            // S09: Rate / concurrency limit not exceeded?
            var rateLimitKey = $"{principal.UserId}:{permission.Value}";
            var rateAllowed = await _rateLimiter.ShouldAllowAsync(rateLimitKey, 100, ct).ConfigureAwait(false);
            if (!rateAllowed)
            {
                decision = AuthorizationDecision.Deny(ErrorCodes.RateLimited, "AUTHZ_P09");
                return decision;
            }

            // S10: Issue single-use AuthorizationTicket
            var ticketTtl = tier switch
            {
                RiskTier.T0 => TimeSpan.FromMinutes(15),
                RiskTier.T1 => TimeSpan.FromMinutes(10),
                RiskTier.T2 => TimeSpan.FromMinutes(5),
                RiskTier.T3 => TimeSpan.FromMinutes(2),
                _ => TimeSpan.FromMinutes(5)
            };

            var ticket = new AuthorizationTicket(
                request.OperationName,
                request.OperationFingerprint,
                request.Provider,
                request.AccountId,
                now.Add(ticketTtl));

            decision = AuthorizationDecision.Allow(ticket, "AUTHZ_P10");
            return decision;
        }
        catch (Exception)
        {
            decision = AuthorizationDecision.Deny(ErrorCodes.InternalDeny, "AUTHZ_PX");
            return decision;
        }
        finally
        {
            // S11: Write audit record for ALLOW / DENY / CHALLENGE (always)
            var auditEntry = new AuditEntry(
                principal?.UserId ?? "ANONYMOUS",
                request.OperationName,
                decision!.Outcome.ToString(),
                decision.ReasonCode,
                decision.PathId,
                request.CorrelationId ?? Guid.NewGuid().ToString("N"),
                null,
                now);

            try
            {
                await _auditWriter.WriteAsync(auditEntry, ct).ConfigureAwait(false);
            }
            catch
            {
                // Never throw from audit in finally; fail closed state is preserved
            }
        }
    }
}
