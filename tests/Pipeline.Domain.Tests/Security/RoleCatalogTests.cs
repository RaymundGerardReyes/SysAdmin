using Pipeline.Domain.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Domain.Tests.Security;

[Trait("Category", "Unit")]
public class RoleCatalogTests
{
    [Fact]
    public void AllRolePermissions_ExistInCatalog()
    {
        foreach (var (roleName, role) in RoleCatalog.Baseline)
        {
            role.Name.ShouldBe(roleName);
            foreach (var permission in role.Permissions)
            {
                PermissionCatalog.Exists(permission).ShouldBeTrue($"Permission {permission.Value} in role {roleName} must exist in PermissionCatalog");
            }
        }
    }

    [Fact]
    public void NoRole_HoldsCreateAndApprovePayments()
    {
        foreach (var (roleName, role) in RoleCatalog.Baseline)
        {
            var hasCreate = role.Permissions.Contains(PermissionCatalog.PaymentsCheckoutCreate);
            var hasApprove = role.Permissions.Contains(PermissionCatalog.PaymentsCheckoutApprove);
            (hasCreate && hasApprove).ShouldBeFalse($"Role '{roleName}' must not hold both payments.checkout.create and payments.checkout.approve");
        }
    }

    [Fact]
    public void T3Permissions_AreHeldOnlyByIntendedRoles()
    {
        var createRoles = RoleCatalog.Baseline.Values
            .Where(r => r.Permissions.Contains(PermissionCatalog.PaymentsCheckoutCreate))
            .Select(r => r.Name)
            .ToList();

        createRoles.ShouldBe(new[] { RoleCatalog.FinanceOperatorRole });

        var approveRoles = RoleCatalog.Baseline.Values
            .Where(r => r.Permissions.Contains(PermissionCatalog.PaymentsCheckoutApprove))
            .Select(r => r.Name)
            .ToList();

        approveRoles.ShouldBe(new[] { RoleCatalog.ApproverRole });
    }
}
