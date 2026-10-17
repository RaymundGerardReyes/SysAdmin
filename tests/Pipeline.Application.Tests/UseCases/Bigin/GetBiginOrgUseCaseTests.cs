using NSubstitute;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.UseCases;
using Pipeline.Application.UseCases.Bigin;
using Pipeline.Domain.Security;
using Pipeline.TestKit.Builders;
using Shouldly;
using Xunit;

namespace Pipeline.Application.Tests.UseCases.Bigin;

[Trait("Category", "Unit")]
public class GetBiginOrgUseCaseTests
{
    [Fact]
    public async Task HandleAsync_WithValidTicket_ReturnsOrganizationDetails()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        credentialBroker.GetAccessTokenAsync(Arg.Any<AuthorizationTicket>(), "Bigin", "default", Arg.Any<CancellationToken>())
            .Returns(new AccessToken("mock_token_123", DateTimeOffset.UtcNow.AddHours(1)));

        var useCase = new GetBiginOrgUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_viewer")
            .WithRoles(RoleCatalog.ViewerRole)
            .Build();

        var ticket = new AuthorizationTicket(
            "bigin.org.read",
            "fp_test",
            "Bigin",
            "default",
            DateTimeOffset.UtcNow.AddMinutes(5));

        var context = new UseCaseContext(principal, ticket, "corr_123");
        var command = new GetBiginOrgCommand("US");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Org.Count.ShouldBe(1);

        var org = result.Value.Org[0];
        org.Id.ShouldBe("2034020000000013208");
        org.CompanyName.ShouldBe("Zylker Travels");
        org.DomainName.ShouldBe("org75407139");
        org.Currency.ShouldBe("US Dollar");
        org.Paid.ShouldBeTrue();
        org.PaidType.ShouldBe("biginpremier");

        await credentialBroker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void GetBiginOrgUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(GetBiginOrgUseCase),
            typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.org.read");

        var perm = new Permission(attr.Permission);
        PermissionCatalog.Exists(perm).ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidDataCenter_ReturnsFailure()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        var useCase = new GetBiginOrgUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_viewer").WithRoles(RoleCatalog.ViewerRole).Build();
        var ticket = new AuthorizationTicket("bigin.org.read", "fp_test", "Bigin", "default", DateTimeOffset.UtcNow.AddMinutes(5));
        var context = new UseCaseContext(principal, ticket, "corr_123");
        var command = new GetBiginOrgCommand("UNKNOWN_DC");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_DATA_CENTER");
    }
}
