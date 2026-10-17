namespace Pipeline.Application.Abstractions;

public enum IdempotencyStatus
{
    InProgress = 0,
    Completed = 1,
    Unknown = 2
}

public sealed record IdempotencyRecord(
    string Key,
    string OperationName,
    string Fingerprint,
    IdempotencyStatus Status,
    string? SerializedResult,
    DateTimeOffset CreatedAtUtc);

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default);
    Task<bool> TryCreateAsync(string key, string operationName, string fingerprint, CancellationToken ct = default);
    Task CompleteAsync(string key, string serializedResult, CancellationToken ct = default);
    Task MarkUnknownAsync(string key, CancellationToken ct = default);
}
