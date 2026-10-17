using System.Text;
using Pipeline.Application.Abstractions;
using Pipeline.Domain.Security;
using Pipeline.Infrastructure.Security;

namespace Pipeline.TestKit.Builders;

public static class PolicyBundleFactory
{
    public static readonly byte[] TestKey = Encoding.UTF8.GetBytes("pipeline_pinned_policy_key_2026_test");

    public static SignedPolicyPayload CreateValid(long version = 1, DateTimeOffset? expiresAt = null)
    {
        var exp = expiresAt ?? DateTimeOffset.UtcNow.AddDays(7);
        var roles = new Dictionary<string, IReadOnlySet<string>>
        {
            ["alice"] = new HashSet<string> { RoleCatalog.OperatorRole },
            ["bob"] = new HashSet<string> { RoleCatalog.FinanceOperatorRole },
            ["charlie"] = new HashSet<string> { RoleCatalog.ApproverRole },
            ["dave"] = new HashSet<string> { RoleCatalog.AuditorRole },
            ["eve"] = new HashSet<string> { RoleCatalog.OwnerRole }
        };

        var scopes = new Dictionary<string, ResourceScope>
        {
            ["alice"] = new ResourceScope(new HashSet<string> { "sheet_123" })
        };

        var sig = SignedPolicyBundleVerifier.CreateSignature(version, exp, roles, TestKey);
        return new SignedPolicyPayload(version, exp, roles, scopes, sig);
    }

    public static SignedPolicyPayload CreateExpired(long version = 1) =>
        CreateValid(version, DateTimeOffset.UtcNow.AddMinutes(-5));

    public static SignedPolicyPayload CreateTampered(long version = 1)
    {
        var valid = CreateValid(version);
        return valid with { Signature = "BAD_SIGNATURE_00000000000000000000000000000000" };
    }

    public static SignedPolicyPayload CreateWithUnknownRole(long version = 2)
    {
        var exp = DateTimeOffset.UtcNow.AddDays(1);
        var roles = new Dictionary<string, IReadOnlySet<string>>
        {
            ["mallory"] = new HashSet<string> { "SuperAdminNonExistent" }
        };
        var sig = SignedPolicyBundleVerifier.CreateSignature(version, exp, roles, TestKey);
        return new SignedPolicyPayload(version, exp, roles, new Dictionary<string, ResourceScope>(), sig);
    }

    public static SignedPolicyPayload CreateWithSoDViolation(long version = 2)
    {
        var exp = DateTimeOffset.UtcNow.AddDays(1);
        var roles = new Dictionary<string, IReadOnlySet<string>>
        {
            ["mallory"] = new HashSet<string> { RoleCatalog.FinanceOperatorRole, RoleCatalog.ApproverRole }
        };
        var sig = SignedPolicyBundleVerifier.CreateSignature(version, exp, roles, TestKey);
        return new SignedPolicyPayload(version, exp, roles, new Dictionary<string, ResourceScope>(), sig);
    }

    public static PolicyBundle CreateInMemoryBundle(long version = 1)
    {
        var p = CreateValid(version);
        return new PolicyBundle(p.Version, p.ExpiresAtUtc, p.UserRoles, p.UserScopes);
    }
}
