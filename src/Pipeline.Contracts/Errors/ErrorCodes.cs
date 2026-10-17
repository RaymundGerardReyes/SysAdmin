namespace Pipeline.Contracts.Errors;

public static class ErrorCodes
{
    public const string AuthnMissing = "AUTHN_MISSING";
    public const string SessionInvalid = "SESSION_INVALID";
    public const string PolicyInvalid = "POLICY_INVALID";
    public const string OperationUnmapped = "OPERATION_UNMAPPED";
    public const string PermissionMissing = "PERMISSION_MISSING";
    public const string ResourceOutOfScope = "RESOURCE_OUT_OF_SCOPE";
    public const string StepupRequired = "STEPUP_REQUIRED";
    public const string ApprovalRequired = "APPROVAL_REQUIRED";
    public const string SodViolation = "SOD_VIOLATION";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalDeny = "INTERNAL_DENY";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string IdempotencyConflict = "IDEMPOTENCY_CONFLICT";
    public const string OperationInProgress = "OPERATION_IN_PROGRESS";
    public const string CredentialDenied = "CREDENTIAL_DENIED";
    public const string EgressBlocked = "EGRESS_BLOCKED";
    public const string NotFound = "NOT_FOUND";
    public const string InvalidRequest = "INVALID_REQUEST";
}
