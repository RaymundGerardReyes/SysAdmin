using System.Collections.Concurrent;
using Pipeline.Application.Abstractions;
using Pipeline.Domain.Jobs;

namespace Pipeline.Infrastructure.Persistence;

internal sealed class SqliteJobStore : IJobStore, IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, JobRecord> _jobs = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, IdempotencyRecord> _idempotency = new(StringComparer.Ordinal);

    public Task<JobRecord?> GetByIdAsync(string jobId, CancellationToken ct = default)
    {
        _jobs.TryGetValue(jobId, out var job);
        return Task.FromResult(job);
    }

    public Task<string> CreateJobAsync(string operationName, string userId, string? idempotencyKey, string? correlationId, CancellationToken ct = default)
    {
        var jobId = $"job_{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        var job = new JobRecord(jobId, operationName, userId, JobStatus.Pending, idempotencyKey, correlationId, now, now);
        _jobs[jobId] = job;
        return Task.FromResult(jobId);
    }

    public Task<bool> UpdateStatusAsync(string jobId, JobStatus newStatus, CancellationToken ct = default)
    {
        if (_jobs.TryGetValue(jobId, out var existing))
        {
            var updated = existing with { Status = newStatus, UpdatedAtUtc = DateTimeOffset.UtcNow };
            _jobs[jobId] = updated;
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<JobRecord>> GetJobsByUserAsync(string userId, CancellationToken ct = default)
    {
        var list = _jobs.Values
            .Where(j => string.Equals(j.UserId, userId, StringComparison.Ordinal))
            .ToList();
        return Task.FromResult<IReadOnlyList<JobRecord>>(list);
    }

    // IIdempotencyStore
    public Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default)
    {
        _idempotency.TryGetValue(key, out var record);
        return Task.FromResult(record);
    }

    public Task<bool> TryCreateAsync(string key, string operationName, string fingerprint, CancellationToken ct = default)
    {
        var record = new IdempotencyRecord(key, operationName, fingerprint, IdempotencyStatus.InProgress, null, DateTimeOffset.UtcNow);
        return Task.FromResult(_idempotency.TryAdd(key, record));
    }

    public Task CompleteAsync(string key, string serializedResult, CancellationToken ct = default)
    {
        if (_idempotency.TryGetValue(key, out var existing))
        {
            var updated = existing with { Status = IdempotencyStatus.Completed, SerializedResult = serializedResult };
            _idempotency[key] = updated;
        }
        return Task.CompletedTask;
    }

    public Task MarkUnknownAsync(string key, CancellationToken ct = default)
    {
        if (_idempotency.TryGetValue(key, out var existing))
        {
            var updated = existing with { Status = IdempotencyStatus.Unknown };
            _idempotency[key] = updated;
        }
        return Task.CompletedTask;
    }
}
