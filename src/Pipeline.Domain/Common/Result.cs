namespace Pipeline.Domain.Common;

public sealed record Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public string? PathId { get; }
    public string? ApprovalId { get; }
    public string Outcome { get; }

    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage, string? pathId, string? approvalId, string outcome)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        PathId = pathId;
        ApprovalId = approvalId;
        Outcome = outcome;
    }

    public static Result<T> Success(T value, string? pathId = null)
        => new(true, value, null, null, pathId, null, "Allow");

    public static Result<T> Failure(string errorCode, string errorMessage, string? pathId = null)
        => new(false, default, errorCode, errorMessage, pathId, null, "Deny");

    public static Result<T> Challenge(string errorCode, string errorMessage, string? pathId = null)
        => new(false, default, errorCode, errorMessage, pathId, null, "Challenge");

    public static Result<T> PendingApproval(string approvalId, string? pathId = null)
        => new(false, default, "APPROVAL_REQUIRED", "Action pending approval.", pathId, approvalId, "PendingApproval");
}
