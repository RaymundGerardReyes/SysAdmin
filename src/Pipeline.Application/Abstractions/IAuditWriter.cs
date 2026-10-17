namespace Pipeline.Application.Abstractions;

public sealed record AuditEntry(
    string UserId,
    string OperationName,
    string Outcome,
    string ReasonCode,
    string? PathId,
    string CorrelationId,
    string? JobId,
    DateTimeOffset TimestampUtc);

public sealed record AuditChainStatus(bool IsIntact, long? FirstBrokenSequence);

public interface IAuditWriter
{
    Task WriteAsync(AuditEntry entry, CancellationToken ct = default);
    Task<AuditChainStatus> VerifyChainAsync(CancellationToken ct = default);
}
