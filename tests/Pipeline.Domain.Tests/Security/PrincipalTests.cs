using Pipeline.Domain.Security;
using Pipeline.TestKit.Builders;
using Shouldly;
using Xunit;

namespace Pipeline.Domain.Tests.Security;

[Trait("Category", "Unit")]
public class PrincipalTests
{
    [Fact]
    public void OperatorPrincipal_HasOperatorPermissions()
    {
        var principal = PrincipalBuilder.For("alice")
            .WithRoles(RoleCatalog.OperatorRole)
            .Build();

        principal.Has(PermissionCatalog.JobsRead).ShouldBeTrue();
        principal.Has(PermissionCatalog.SheetsValuesWrite).ShouldBeTrue();
        principal.Has(PermissionCatalog.PaymentsCheckoutCreate).ShouldBeFalse();
    }

    [Fact]
    public void ViewerPrincipal_CannotWrite()
    {
        var principal = PrincipalBuilder.For("bob")
            .WithRoles(RoleCatalog.ViewerRole)
            .Build();

        principal.Has(PermissionCatalog.JobsRead).ShouldBeTrue();
        principal.Has(PermissionCatalog.SheetsValuesRead).ShouldBeTrue();
        principal.Has(PermissionCatalog.SheetsValuesWrite).ShouldBeFalse();
        principal.Has(PermissionCatalog.GmailMessagesSend).ShouldBeFalse();
    }

    [Fact]
    public void MultiRolePrincipal_CombinesPermissions()
    {
        var principal = PrincipalBuilder.For("charlie")
            .WithRoles(RoleCatalog.ViewerRole, RoleCatalog.IntegrationAdminRole)
            .Build();

        principal.Has(PermissionCatalog.SheetsValuesRead).ShouldBeTrue();
        principal.Has(PermissionCatalog.IntegrationsConnect).ShouldBeTrue();
        principal.Has(PermissionCatalog.SheetsValuesWrite).ShouldBeFalse();
    }
}
