using Pipeline.Domain.Jobs;
using Pipeline.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace Pipeline.Integration.Tests.Persistence;

[Trait("Category", "Integration")]
public class SqliteJobStoreTests
{
    [Fact]
    public async Task IdempotencyKey_UniqueConstraint_Enforced()
    {
        var store = new SqliteJobStore();
        var first = await store.TryCreateAsync("key_123", "orders.create", "fingerprint_1");
        first.ShouldBeTrue();

        var second = await store.TryCreateAsync("key_123", "orders.create", "fingerprint_1");
        second.ShouldBeFalse();
    }

    [Fact]
    public async Task ConcurrentSubmissions_SameKey_OnlyOneSucceeds()
    {
        var store = new SqliteJobStore();
        var key = "concurrent_key_999";

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => store.TryCreateAsync(key, "op", "fp"))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        results.Count(r => r).ShouldBe(1);
    }

    [Fact]
    public async Task StateTransitions_ArePersistedAndOrdered()
    {
        var store = new SqliteJobStore();
        var jobId = await store.CreateJobAsync("sheets.read", "user_1", "idem_key", "corr_1");

        var job = await store.GetByIdAsync(jobId);
        job.ShouldNotBeNull();
        job.Status.ShouldBe(JobStatus.Pending);

        await store.UpdateStatusAsync(jobId, JobStatus.Running);
        var updated = await store.GetByIdAsync(jobId);
        updated!.Status.ShouldBe(JobStatus.Running);
    }
}
