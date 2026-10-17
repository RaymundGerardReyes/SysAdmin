namespace Pipeline.Contracts.Requests;

public sealed record ReadSheetRangeRequest(
    string SpreadsheetId,
    string Range);

public sealed record WriteSheetRangeRequest(
    string SpreadsheetId,
    string Range,
    IReadOnlyList<IReadOnlyList<string>> Values,
    string IdempotencyKey);

public sealed record SendGmailMessageRequest(
    string RecipientEmail,
    string Subject,
    string Body,
    string IdempotencyKey);

public sealed record CreatePaymentCheckoutRequest(
    string AccountId,
    decimal Amount,
    string Currency,
    string Reference,
    string IdempotencyKey);

public sealed record ApprovePaymentCheckoutRequest(
    string ApprovalId,
    string CheckoutId);

public sealed record ManageUserRolesRequest(
    string TargetUserId,
    IReadOnlySet<string> NewRoleNames);
