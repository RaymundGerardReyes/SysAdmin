namespace Pipeline.Contracts.Common;

public sealed record ResultDto<T>(
    bool IsSuccess,
    T? Value,
    string? ErrorCode,
    string? ErrorMessage,
    string? PathId,
    string? ApprovalId);
