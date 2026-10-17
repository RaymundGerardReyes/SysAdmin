using Pipeline.Infrastructure.Security;
using Pipeline.TestKit.Builders;
using Shouldly;
using Xunit;

namespace Pipeline.Security.Tests.Rbac;

[Trait("Category", "Security")]
public class PolicyTamperingTests
{
    private readonly SignedPolicyBundleVerifier _verifier = new(PolicyBundleFactory.TestKey, initialVersion: 5);

    [Fact]
    public void SEC_POLICY_TamperedBundle_Rejected()
    {
        var tampered = PolicyBundleFactory.CreateTampered(version: 6);
        var success = _verifier.Verify(tampered, DateTimeOffset.UtcNow, out _, out var reason);

        success.ShouldBeFalse();
        reason.ShouldBe("INVALID_SIGNATURE");
    }

    [Fact]
    public void SEC_POLICY_ExpiredBundle_FailsClosed()
    {
        var expired = PolicyBundleFactory.CreateExpired(version: 6);
        var success = _verifier.Verify(expired, DateTimeOffset.UtcNow, out _, out var reason);

        success.ShouldBeFalse();
        reason.ShouldBe("POLICY_EXPIRED");
    }

    [Fact]
    public void SEC_POLICY_OlderVersion_RollbackRejected()
    {
        // Current seen version is 5; attacker supplies version 4
        var rolledBack = PolicyBundleFactory.CreateValid(version: 4);
        var success = _verifier.Verify(rolledBack, DateTimeOffset.UtcNow, out _, out var reason);

        success.ShouldBeFalse();
        reason.ShouldBe("POLICY_ROLLBACK_DETECTED");
    }

    [Fact]
    public void SEC_POLICY_UnknownRoleOrPermission_Rejected()
    {
        var unknownRole = PolicyBundleFactory.CreateWithUnknownRole(version: 6);
        var success = _verifier.Verify(unknownRole, DateTimeOffset.UtcNow, out _, out var reason);

        success.ShouldBeFalse();
        reason!.ShouldContain("UNKNOWN_ROLE");
    }
}
