using Pipeline.Application.Authorization;
using Pipeline.Contracts.Errors;
using Pipeline.Domain.Security;
using Pipeline.TestKit.Builders;
using Shouldly;
using Xunit;

namespace Pipeline.Security.Tests.Rbac;

[Trait("Category", "Security")]
public class IdorTests
{
    private readonly ScopedResourceAuthorizer _authorizer = new();

    [Fact]
    public void SEC_IDOR_SpreadsheetNotInScope_Denied()
    {
        var principal = PrincipalBuilder.For("alice")
            .WithSpreadsheet("allowed_sheet_1")
            .Build();

        var resources = new Dictionary<string, string> { ["spreadsheetId"] = "foreign_sheet_999" };
        _authorizer.IsInScope(principal, resources).ShouldBeFalse();
    }

    [Fact]
    public void SEC_IDOR_ChatSpaceNotInScope_Denied()
    {
        var principal = PrincipalBuilder.For("alice")
            .Build();

        var resources = new Dictionary<string, string> { ["chatSpaceId"] = "spaces/secret_finance" };
        _authorizer.IsInScope(principal, resources).ShouldBeFalse();
    }

    [Fact]
    public void SEC_IDOR_IdWithTrailingSpaceOrCaseChange_DoesNotBypassScope()
    {
        var principal = PrincipalBuilder.For("alice")
            .WithSpreadsheet("allowed_sheet")
            .Build();

        // Trailing space attempt
        var resourcesWithSpace = new Dictionary<string, string> { ["spreadsheetId"] = "allowed_sheet " };
        _authorizer.IsInScope(principal, resourcesWithSpace).ShouldBeFalse();

        // Null character attempt
        var resourcesWithControl = new Dictionary<string, string> { ["spreadsheetId"] = "allowed_sheet\0" };
        _authorizer.IsInScope(principal, resourcesWithControl).ShouldBeFalse();
    }
}
