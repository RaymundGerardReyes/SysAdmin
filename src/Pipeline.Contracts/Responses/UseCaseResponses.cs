namespace Pipeline.Contracts.Responses;

public sealed record SheetRangeResponse(
    string Range,
    IReadOnlyList<IReadOnlyList<string>> Values);

public sealed record SheetWriteResponse(
    int UpdatedRows,
    int UpdatedColumns,
    int UpdatedCells);

public sealed record GmailMessageResponse(
    string MessageId,
    string ThreadId,
    DateTimeOffset SentAtUtc);

public sealed record PaymentCheckoutResponse(
    string CheckoutId,
    string Status,
    string? CheckoutUrl);

public sealed record PaymentApprovalResponse(
    string CheckoutId,
    bool Approved,
    string ApproverUserId,
    DateTimeOffset ApprovedAtUtc);

public sealed record ManageUserRolesResponse(
    string TargetUserId,
    IReadOnlySet<string> AssignedRoles,
    DateTimeOffset UpdatedAtUtc);
