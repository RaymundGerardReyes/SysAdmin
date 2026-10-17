using System.Text.Json;
using Pipeline.Application.Abstractions;
using Pipeline.Contracts.Errors;
using Pipeline.Domain.Common;

namespace Pipeline.Application.Pipeline.Behaviors;

public sealed class IdempotencyBehavior
{
    private readonly IIdempotencyStore _idempotencyStore;

    public IdempotencyBehavior(IIdempotencyStore idempotencyStore)
    {
        _idempotencyStore = idempotencyStore;
    }

    public async Task<(Result<TResponse>? ExistingResult, bool RequiresExecution)> CheckAsync<TRequest, TResponse>(
        TRequest request,
        string fingerprint,
        CancellationToken ct = default)
        where TRequest : IUseCaseRequest
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return (null, true);
        }

        var key = request.IdempotencyKey;
        var existing = await _idempotencyStore.GetAsync(key, ct).ConfigureAwait(false);

        if (existing is null)
        {
            // IDEM_P01: New key
            await _idempotencyStore.TryCreateAsync(key, request.OperationName, fingerprint, ct).ConfigureAwait(false);
            return (null, true);
        }

        // IDEM_P04: Same key, different fingerprint => 409 Conflict
        if (!string.Equals(existing.Fingerprint, fingerprint, StringComparison.Ordinal))
        {
            return (Result<TResponse>.Failure(ErrorCodes.IdempotencyConflict, "Idempotency key reuse with differing fingerprint.", "IDEM_P04"), false);
        }

        // IDEM_P03: Same key, same fingerprint, still running => return in progress
        if (existing.Status == IdempotencyStatus.InProgress)
        {
            return (Result<TResponse>.Failure(ErrorCodes.OperationInProgress, "Operation with this idempotency key is already in progress.", "IDEM_P03"), false);
        }

        // IDEM_P02: Same key, same fingerprint, completed => return stored result
        if (existing.Status == IdempotencyStatus.Completed && existing.SerializedResult is not null)
        {
            try
            {
                var val = JsonSerializer.Deserialize<TResponse>(existing.SerializedResult);
                return (Result<TResponse>.Success(val!, "IDEM_P02"), false);
            }
            catch
            {
                // Fall back if deserialization fails
            }
        }

        return (null, true);
    }

    public async Task CompleteAsync<TResponse>(string? idempotencyKey, TResponse response, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return;
        var serialized = JsonSerializer.Serialize(response);
        await _idempotencyStore.CompleteAsync(idempotencyKey, serialized, ct).ConfigureAwait(false);
    }

    public async Task MarkUnknownAsync(string? idempotencyKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) return;
        // IDEM_P05: Ambiguous timeout marks unknown
        await _idempotencyStore.MarkUnknownAsync(idempotencyKey, ct).ConfigureAwait(false);
    }
}
