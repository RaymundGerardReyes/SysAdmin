using Pipeline.Domain.Jobs;

namespace Pipeline.Application.Abstractions;

public sealed record JobRecord(
    string JobId,
    string OperationName,
    string UserId,
    JobStatus Status,
    string? IdempotencyKey,
    string? CorrelationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public interface IJobStore
{
    Task<JobRecord?> GetByIdAsync(string jobId, CancellationToken ct = default);
    Task<string> CreateJobAsync(string operationName, string userId, string? idempotencyKey, string? correlationId, CancellationToken ct = default);
    Task<bool> UpdateStatusAsync(string jobId, JobStatus newStatus, CancellationToken ct = default);
    Task<IReadOnlyList<JobRecord>> GetJobsByUserAsync(string userId, CancellationToken ct = default);
}
