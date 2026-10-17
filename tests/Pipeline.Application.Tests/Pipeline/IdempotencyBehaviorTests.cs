using Pipeline.Application.Pipeline;
using Pipeline.Application.Pipeline.Behaviors;
using Pipeline.Contracts.Errors;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;
using Pipeline.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Pipeline.Application.Tests.Pipeline;

public sealed record DummyIdempotentRequest(string OperationName, string? IdempotencyKey) : IUseCaseRequest;

[Trait("Category", "Path")]
public class IdempotencyBehaviorTests
{
    private readonly SqliteJobStore _store = new();

    [Fact]
    public async Task IDEM_P01_NewKey_ExecutesAndStores()
    {
        var behavior = new IdempotencyBehavior(_store);
        var req = new DummyIdempotentRequest("orders.create", "key_1");
        var fingerprint = "fp_abc";

        var (existing, requiresExec) = await behavior.CheckAsync<DummyIdempotentRequest, SheetWriteResponse>(req, fingerprint);
        requiresExec.ShouldBeTrue();
        existing.ShouldBeNull();

        await behavior.CompleteAsync("key_1", new SheetWriteResponse(1, 2, 2));

        var record = await _store.GetAsync("key_1");
        record.ShouldNotBeNull();
        record.Status.ShouldBe(Application.Abstractions.IdempotencyStatus.Completed);
    }

    [Fact]
    public async Task IDEM_P02_SameKeySameFingerprintCompleted_ReturnsStoredResult()
    {
        var behavior = new IdempotencyBehavior(_store);
        var req = new DummyIdempotentRequest("orders.create", "key_2");
        var fingerprint = "fp_abc";

        // Seed first execution
        await behavior.CheckAsync<DummyIdempotentRequest, SheetWriteResponse>(req, fingerprint);
        await behavior.CompleteAsync("key_2", new SheetWriteResponse(5, 5, 25));

        // Second call with same key and same fingerprint
        var (existing, requiresExec) = await behavior.CheckAsync<DummyIdempotentRequest, SheetWriteResponse>(req, fingerprint);
        requiresExec.ShouldBeFalse();
        existing.ShouldNotBeNull();
        existing.IsSuccess.ShouldBeTrue();
        existing.PathId.ShouldBe("IDEM_P02");
        existing.Value.ShouldNotBeNull();
        existing.Value.UpdatedCells.ShouldBe(25);
    }

    [Fact]
    public async Task IDEM_P03_SameKeySameFingerprintRunning_ReturnsInProgress()
    {
        var behavior = new IdempotencyBehavior(_store);
        var req = new DummyIdempotentRequest("orders.create", "key_3");
        var fingerprint = "fp_abc";

        // Start first execution (leaves in InProgress state)
        await behavior.CheckAsync<DummyIdempotentRequest, SheetWriteResponse>(req, fingerprint);

        // Concurrent/second call before completion
        var (existing, requiresExec) = await behavior.CheckAsync<DummyIdempotentRequest, SheetWriteResponse>(req, fingerprint);
        requiresExec.ShouldBeFalse();
        existing.ShouldNotBeNull();
        existing.IsFailure.ShouldBeTrue();
        existing.ErrorCode.ShouldBe(ErrorCodes.OperationInProgress);
        existing.PathId.ShouldBe("IDEM_P03");
    }

    [Fact]
    public async Task IDEM_P04_SameKeyDifferentFingerprint_Returns409()
    {
        var behavior = new IdempotencyBehavior(_store);
        var req1 = new DummyIdempotentRequest("orders.create", "key_4");
        await behavior.CheckAsync<DummyIdempotentRequest, SheetWriteResponse>(req1, "fingerprint_original");

        // Reused key with DIFFERENT fingerprint
        var req2 = new DummyIdempotentRequest("orders.create", "key_4");
        var (existing, requiresExec) = await behavior.CheckAsync<DummyIdempotentRequest, SheetWriteResponse>(req2, "fingerprint_altered");

        requiresExec.ShouldBeFalse();
        existing.ShouldNotBeNull();
        existing.IsFailure.ShouldBeTrue();
        existing.ErrorCode.ShouldBe(ErrorCodes.IdempotencyConflict);
        existing.PathId.ShouldBe("IDEM_P04");
    }

    [Fact]
    public async Task IDEM_P05_AmbiguousTimeout_MarksUnknown_NoBlindRetry()
    {
        var behavior = new IdempotencyBehavior(_store);
        var req = new DummyIdempotentRequest("orders.create", "key_5");
        var fingerprint = "fp_timeout";

        await behavior.CheckAsync<DummyIdempotentRequest, SheetWriteResponse>(req, fingerprint);

        // Mark unknown on timeout
        await behavior.MarkUnknownAsync("key_5");

        var record = await _store.GetAsync("key_5");
        record.ShouldNotBeNull();
        record.Status.ShouldBe(Application.Abstractions.IdempotencyStatus.Unknown);
    }
}
