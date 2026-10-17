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

public sealed record BiginModuleItemResponse(
    string Id,
    string ApiName,
    string ModuleName,
    string SingularLabel,
    string PluralLabel,
    bool ApiSupported,
    bool Creatable,
    bool Deletable,
    bool Editable,
    bool Viewable,
    bool Visible,
    int Visibility,
    string GeneratedType,
    int SequenceNumber,
    bool QuickCreate);

public sealed record BiginModulesResponseContract(
    IReadOnlyList<BiginModuleItemResponse> Modules);

public sealed record BiginOrgDetailsContract(
    string Id,
    string? CompanyName,
    string? DomainName,
    string? TimeZone,
    string? Currency,
    string? CurrencySymbol,
    string? PrimaryEmail,
    string? Phone,
    string? Country,
    string? City,
    bool McStatus,
    bool Paid,
    string? PaidType,
    int? UsersLicensePurchased);

public sealed record BiginOrgResponseContract(
    IReadOnlyList<BiginOrgDetailsContract> Org);

public sealed record BiginPhotoUploadResponseContract(
    string Code,
    string Message,
    string Status);

public sealed record BiginRelatedRecordsResponseContract(
    IReadOnlyList<System.Text.Json.JsonElement> Records,
    int Page,
    int PerPage,
    int Count,
    bool MoreRecords);
