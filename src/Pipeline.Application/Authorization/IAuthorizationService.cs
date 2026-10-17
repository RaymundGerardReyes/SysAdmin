namespace Pipeline.Application.Authorization;

public sealed record AuthorizationRequest(
    string OperationName,
    string RequiredPermission,
    string OperationFingerprint,
    string? Provider,
    string? AccountId,
    IReadOnlyDictionary<string, string> ResourceIds,
    string? ApprovalId = null,
    string? CorrelationId = null);

public interface IAuthorizationService
{
    Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken ct = default);
}
