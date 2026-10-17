using Pipeline.Application.Abstractions;

namespace Pipeline.Application.Pipeline.Behaviors;

public sealed class AuditBehavior
{
    private readonly IAuditWriter _auditWriter;
    private readonly IClock _clock;

    public AuditBehavior(IAuditWriter auditWriter, IClock clock)
    {
        _auditWriter = auditWriter;
        _clock = clock;
    }

    public async Task AuditStartAsync(string userId, string operationName, string correlationId, CancellationToken ct = default)
    {
        var entry = new AuditEntry(
            userId,
            operationName,
            "Start",
            "PIPELINE_START",
            null,
            correlationId,
            null,
            _clock.UtcNow);

        await _auditWriter.WriteAsync(entry, ct).ConfigureAwait(false);
    }

    public async Task AuditCompletionAsync(
        string userId,
        string operationName,
        string outcome,
        string reasonCode,
        string? pathId,
        string correlationId,
        CancellationToken ct = default)
    {
        var entry = new AuditEntry(
            userId,
            operationName,
            outcome,
            reasonCode,
            pathId,
            correlationId,
            null,
            _clock.UtcNow);

        await _auditWriter.WriteAsync(entry, ct).ConfigureAwait(false);
    }
}
