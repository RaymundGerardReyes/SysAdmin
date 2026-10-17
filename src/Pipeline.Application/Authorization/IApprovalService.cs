namespace Pipeline.Application.Authorization;

public sealed record ApprovalResult(bool IsValid, string ReasonCode, string? ApproverUserId = null);

public interface IApprovalService
{
    Task<ApprovalResult> ValidateAsync(
        string approvalId,
        string operationFingerprint,
        string requesterUserId,
        CancellationToken ct = default);

    Task<string> RequestAsync(
        string operationFingerprint,
        string requesterUserId,
        CancellationToken ct = default);
}
