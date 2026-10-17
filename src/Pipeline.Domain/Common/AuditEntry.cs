namespace Pipeline.Domain.Common;

public sealed record AuditEntry(
    string UserId,
    string OperationName,
    string Outcome,
    string ReasonCode,
    string? PathId,
    string CorrelationId,
    string? PrevHash,
    DateTimeOffset TimestampUtc);
