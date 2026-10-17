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

public sealed record GetBiginModulesRequest(
    string? DataCenter = null);

public sealed record GetBiginOrgRequest(
    string? DataCenter = null);

public sealed record UploadBiginOrgPhotoRequest(
    string? DataCenter,
    byte[] FileBytes,
    string FileName,
    string ContentType = "image/png");

public sealed record GetBiginRelatedRecordsRequest(
    string ModuleApiName,
    string RecordId,
    string RelatedListApiName,
    IReadOnlyList<string>? Fields = null,
    int? Page = 1,
    int? PerPage = 200,
    string? DataCenter = null);

