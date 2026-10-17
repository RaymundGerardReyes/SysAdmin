using System.Security.Cryptography;
using System.Text;
using Pipeline.Application.Abstractions;

namespace Pipeline.Infrastructure.Security;

internal sealed record ChainedAuditRow(
    long Sequence,
    string PrevHash,
    string Hash,
    AuditEntry Entry);

internal sealed class HashChainAuditWriter : IAuditWriter
{
    private readonly List<ChainedAuditRow> _rows = new();
    private readonly object _lock = new();

    public Task WriteAsync(AuditEntry entry, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var seq = _rows.Count + 1;
            var prevHash = _rows.Count > 0 ? _rows[^1].Hash : "GENESIS_0000000000000000000000000000000000000000000000000000000000000000";
            var currentHash = ComputeHash(prevHash, entry);
            _rows.Add(new ChainedAuditRow(seq, prevHash, currentHash, entry));
        }

        return Task.CompletedTask;
    }

    public Task<AuditChainStatus> VerifyChainAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            var expectedPrev = "GENESIS_0000000000000000000000000000000000000000000000000000000000000000";
            for (var i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                var expectedSequence = (long)(i + 1);
                if (row.Sequence != expectedSequence || !string.Equals(row.PrevHash, expectedPrev, StringComparison.Ordinal))
                {
                    return Task.FromResult(new AuditChainStatus(false, expectedSequence));
                }

                var recomputed = ComputeHash(row.PrevHash, row.Entry);
                if (!string.Equals(row.Hash, recomputed, StringComparison.Ordinal))
                {
                    return Task.FromResult(new AuditChainStatus(false, expectedSequence));
                }

                expectedPrev = row.Hash;
            }

            return Task.FromResult(new AuditChainStatus(true, null));
        }
    }

    // Direct mutators for Security/Audit testing only
    internal void TamperRow(int index, AuditEntry tamperedEntry)
    {
        lock (_lock)
        {
            if (index >= 0 && index < _rows.Count)
            {
                var current = _rows[index];
                _rows[index] = new ChainedAuditRow(current.Sequence, current.PrevHash, current.Hash, tamperedEntry);
            }
        }
    }

    internal void DeleteRow(int index)
    {
        lock (_lock)
        {
            if (index >= 0 && index < _rows.Count)
            {
                _rows.RemoveAt(index);
            }
        }
    }

    internal void ReorderRows(int index1, int index2)
    {
        lock (_lock)
        {
            if (index1 >= 0 && index1 < _rows.Count && index2 >= 0 && index2 < _rows.Count)
            {
                (_rows[index1], _rows[index2]) = (_rows[index2], _rows[index1]);
            }
        }
    }

    private static string ComputeHash(string prevHash, AuditEntry entry)
    {
        var canonical = $"{prevHash}|{entry.UserId}|{entry.OperationName}|{entry.Outcome}|{entry.ReasonCode}|{entry.PathId ?? ""}|{entry.CorrelationId}|{entry.TimestampUtc.ToUnixTimeSeconds()}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes);
    }
}
