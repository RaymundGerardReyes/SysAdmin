using FsCheck;
using FsCheck.Xunit;
using Pipeline.Domain.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Domain.Tests.Security;

[Trait("Category", "Unit")]
public class SeparationOfDutiesRuleTests
{
    [Fact]
    public void Approver_SameAsRequester_NotAllowed()
    {
        SeparationOfDutiesRule.IsApproverAllowed("alice", "alice").ShouldBeFalse();
        SeparationOfDutiesRule.IsApproverAllowed("alice ", "alice").ShouldBeFalse();
        SeparationOfDutiesRule.IsApproverAllowed("bob", "bob").ShouldBeFalse();
    }

    [Fact]
    public void Approver_DifferentUser_Allowed()
    {
        SeparationOfDutiesRule.IsApproverAllowed("alice", "bob").ShouldBeTrue();
        SeparationOfDutiesRule.IsApproverAllowed("requester_1", "approver_2").ShouldBeTrue();
    }

    [Fact]
    public void Auditor_WithWritePermission_InvalidRoleSet()
    {
        var invalidRoles = new HashSet<string> { RoleCatalog.AuditorRole, RoleCatalog.OperatorRole };
        SeparationOfDutiesRule.IsRoleSetValid(invalidRoles).ShouldBeFalse();
    }

    [Fact]
    public void FinanceAndApproverTogether_InvalidRoleSet()
    {
        var invalidRoles = new HashSet<string> { RoleCatalog.FinanceOperatorRole, RoleCatalog.ApproverRole };
        SeparationOfDutiesRule.IsRoleSetValid(invalidRoles).ShouldBeFalse();
    }

    [Property]
    public Property ApproverDifferent_AlwaysAllowed(NonNull<string> a, NonNull<string> b)
    {
        var idA = a.Get.Trim();
        var idB = b.Get.Trim();

        if (string.IsNullOrEmpty(idA) || string.IsNullOrEmpty(idB))
        {
            return true.ToProperty();
        }

        var expected = !string.Equals(idA, idB, StringComparison.Ordinal);
        return (SeparationOfDutiesRule.IsApproverAllowed(idA, idB) == expected).ToProperty();
    }
}
