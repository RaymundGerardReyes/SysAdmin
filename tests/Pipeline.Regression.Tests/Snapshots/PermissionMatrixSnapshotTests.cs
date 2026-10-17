using System.Text;
using Pipeline.Domain.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Regression.Tests.Snapshots;

[Trait("Category", "Regression")]
public class PermissionMatrixSnapshotTests
{
    [Fact]
    public void RoleCatalog_Matrix_MatchesApprovedSnapshot()
    {
        var sb = new StringBuilder();
        foreach (var (roleName, role) in RoleCatalog.Baseline.OrderBy(r => r.Key, StringComparer.Ordinal))
        {
            sb.AppendLine($"Role: {roleName}");
            foreach (var perm in role.Permissions.OrderBy(p => p.Value, StringComparer.Ordinal))
            {
                sb.AppendLine($"  - {perm.Value}");
            }
        }

        var rendered = sb.ToString().TrimEnd();

        var snapshotPath = Path.Combine(AppContext.BaseDirectory, "Snapshots", "PermissionMatrix.verified.txt");
        if (!File.Exists(snapshotPath))
        {
            snapshotPath = Path.Combine("Snapshots", "PermissionMatrix.verified.txt");
        }

        var expected = File.ReadAllText(snapshotPath).TrimEnd();
        // Normalize CRLF to LF
        var normalizedRendered = rendered.Replace("\r\n", "\n");
        var normalizedExpected = expected.Replace("\r\n", "\n");

        normalizedRendered.ShouldBe(normalizedExpected, "RoleCatalog permission matrix drifted from approved security snapshot!");
    }
}
