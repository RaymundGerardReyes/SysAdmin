using Pipeline.Application.Abstractions;
using Pipeline.Infrastructure.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Security.Tests.Audit;

[Trait("Category", "Security")]
public class AuditTamperTests
{
    private readonly HashChainAuditWriter _writer = new();

    private async Task PopulateInitialEntriesAsync(int count = 5)
    {
        for (var i = 1; i <= count; i++)
        {
            var entry = new AuditEntry(
                $"user_{i}",
                $"operation_{i}",
                "Allow",
                "OK",
                "AUTHZ_P10",
                $"corr_{i}",
                null,
                DateTimeOffset.UtcNow.AddMinutes(i));

            await _writer.WriteAsync(entry);
        }
    }

    [Fact]
    public async Task CleanChain_VerifiesSuccessfully()
    {
        await PopulateInitialEntriesAsync(5);
        var status = await _writer.VerifyChainAsync();

        status.IsIntact.ShouldBeTrue();
        status.FirstBrokenSequence.ShouldBeNull();
    }

    [Fact]
    public async Task SEC_AUDIT_EditedRow_ChainBreaksAtThatSequence()
    {
        await PopulateInitialEntriesAsync(5);

        // Tamper with row 3
        var tampered = new AuditEntry("attacker", "tampered_op", "Allow", "OK", "AUTHZ_P10", "corr_3", null, DateTimeOffset.UtcNow);
        _writer.TamperRow(2, tampered); // 0-based index 2 = sequence 3

        var status = await _writer.VerifyChainAsync();
        status.IsIntact.ShouldBeFalse();
        status.FirstBrokenSequence.ShouldBe(3);
    }

    [Fact]
    public async Task SEC_AUDIT_DeletedRow_ChainBreaks()
    {
        await PopulateInitialEntriesAsync(5);

        // Delete row 2
        _writer.DeleteRow(1);

        var status = await _writer.VerifyChainAsync();
        status.IsIntact.ShouldBeFalse();
        status.FirstBrokenSequence.ShouldBe(2);
    }

    [Fact]
    public async Task SEC_AUDIT_ReorderedRows_ChainBreaks()
    {
        await PopulateInitialEntriesAsync(5);

        // Swap row 1 and row 2
        _writer.ReorderRows(1, 2);

        var status = await _writer.VerifyChainAsync();
        status.IsIntact.ShouldBeFalse();
        status.FirstBrokenSequence.ShouldBe(2);
    }
}
