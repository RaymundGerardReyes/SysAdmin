using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Pipeline.Application.Authorization;
using Pipeline.Contracts.Errors;
using Pipeline.Domain.Common;

namespace Pipeline.Application.Pipeline.Behaviors;

public sealed class AuthorizationBehavior
{
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationBehavior(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task<(Result<TResponse>? FailureResult, AuthorizationTicket? Ticket)> AuthorizeAsync<TRequest, TResponse>(
        TRequest request,
        Type handlerType,
        CancellationToken ct = default)
        where TRequest : IUseCaseRequest
    {
        var attr = handlerType.GetCustomAttribute<RequiresPermissionAttribute>();
        if (attr is null)
        {
            return (Result<TResponse>.Failure(ErrorCodes.OperationUnmapped, "Handler lacks RequiresPermission attribute.", "AUTHZ_P04"), null);
        }

        var fingerprint = ComputeFingerprint(request);
        var resourceIds = request.GetResourceIds();
        string? provider = null;
        string? accountId = null;

        if (resourceIds.TryGetValue("provider", out var p)) provider = p;
        if (resourceIds.TryGetValue("accountId", out var a)) accountId = a;

        var authRequest = new AuthorizationRequest(
            request.OperationName,
            attr.Permission,
            fingerprint,
            provider,
            accountId,
            resourceIds,
            null,
            request.CorrelationId);

        var decision = await _authorizationService.AuthorizeAsync(authRequest, ct).ConfigureAwait(false);

        if (!decision.IsAllowed)
        {
            return decision.Outcome switch
            {
                DecisionOutcome.Challenge => (Result<TResponse>.Challenge(decision.ReasonCode, "Authorization step-up required.", decision.PathId), null),
                DecisionOutcome.PendingApproval => (Result<TResponse>.PendingApproval(decision.ReasonCode, decision.PathId), null),
                _ => (Result<TResponse>.Failure(decision.ReasonCode, "Authorization denied.", decision.PathId), null)
            };
        }

        return (null, decision.Ticket);
    }

    public static string ComputeFingerprint(IUseCaseRequest request)
    {
        var canonical = $"{request.OperationName}|{request.IdempotencyKey ?? ""}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes);
    }
}
