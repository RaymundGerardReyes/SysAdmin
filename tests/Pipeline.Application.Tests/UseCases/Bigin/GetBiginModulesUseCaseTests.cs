using NSubstitute;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.UseCases;
using Pipeline.Application.UseCases.Bigin;
using Pipeline.Domain.Security;
using Shouldly;
using Xunit;

using Pipeline.TestKit.Builders;

namespace Pipeline.Application.Tests.UseCases.Bigin;

[Trait("Category", "Unit")]
public class GetBiginModulesUseCaseTests
{
    [Fact]
    public async Task HandleAsync_WithValidTicket_ReturnsModuleList()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        credentialBroker.GetAccessTokenAsync(Arg.Any<AuthorizationTicket>(), "Bigin", "default", Arg.Any<CancellationToken>())
            .Returns(new AccessToken("mock_token_123", DateTimeOffset.UtcNow.AddHours(1)));

        var useCase = new GetBiginModulesUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_viewer")
            .WithRoles(RoleCatalog.ViewerRole)
            .Build();

        // AuthorizationTicket constructor is internal; since InternalsVisibleTo is enabled for Pipeline.Application.Tests,
        // we can instantiate AuthorizationTicket directly.
        var ticket = new AuthorizationTicket(
            "bigin.modules.read",
            "fp_test",
            "Bigin",
            "default",
            DateTimeOffset.UtcNow.AddMinutes(5));

        var context = new UseCaseContext(principal, ticket, "corr_123");
        var command = new GetBiginModulesCommand("US");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Modules.Count.ShouldBe(11);

        // Verify Accounts and Contacts
        var accounts = result.Value.Modules.FirstOrDefault(m => m.ApiName == "Accounts");
        accounts.ShouldNotBeNull();
        accounts.SingularLabel.ShouldBe("Company");
        accounts.PluralLabel.ShouldBe("Companies");

        var contacts = result.Value.Modules.FirstOrDefault(m => m.ApiName == "Contacts");
        contacts.ShouldNotBeNull();
        contacts.SingularLabel.ShouldBe("Contact");

        await credentialBroker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetBiginModulesUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(GetBiginModulesUseCase),
            typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.contacts.read");

        var perm = new Permission(attr.Permission);
        PermissionCatalog.Exists(perm).ShouldBeTrue();
    }

    [Fact]
    public void GetBiginModulesCommand_HasExpectedOperationNameAndResourceIds()
    {
        var cmd = new GetBiginModulesCommand("EU");
        cmd.OperationName.ShouldBe("bigin.modules.read");
        cmd.DataCenter.ShouldBe("EU");

        var resIds = cmd.GetResourceIds();
        resIds.ShouldNotBeNull();
        resIds["provider"].ShouldBe("Bigin");
        resIds["accountId"].ShouldBe("default");
    }

    [Fact]
    public async Task HandleAsync_WithEuDataCenter_ReturnsModulesSuccessfully()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        credentialBroker.GetAccessTokenAsync(Arg.Any<AuthorizationTicket>(), "Bigin", "default", Arg.Any<CancellationToken>())
            .Returns(new AccessToken("mock_eu_token", DateTimeOffset.UtcNow.AddHours(1)));

        var useCase = new GetBiginModulesUseCase(credentialBroker);
        var principal = PrincipalBuilder.For("user_eu").WithRoles(RoleCatalog.ViewerRole).Build();
        var ticket = new AuthorizationTicket("bigin.modules.read", "fp_eu", "Bigin", "default", DateTimeOffset.UtcNow.AddMinutes(5));
        var context = new UseCaseContext(principal, ticket, "corr_eu");
        var command = new GetBiginModulesCommand("EU");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Modules.Count.ShouldBe(11);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidDataCenter_ReturnsFailure()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        var useCase = new GetBiginModulesUseCase(credentialBroker);
        var principal = PrincipalBuilder.For("user_test").WithRoles(RoleCatalog.ViewerRole).Build();
        var ticket = new AuthorizationTicket("bigin.modules.read", "fp_test", "Bigin", "default", DateTimeOffset.UtcNow.AddMinutes(5));
        var context = new UseCaseContext(principal, ticket, "corr_test");
        var command = new GetBiginModulesCommand("INVALID_DC");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_DATA_CENTER");
    }
}
