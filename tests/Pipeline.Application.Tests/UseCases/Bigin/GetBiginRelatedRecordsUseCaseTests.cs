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
public class GetBiginRelatedRecordsUseCaseTests
{
    [Fact]
    public async Task HandleAsync_WithValidTicket_ReturnsRelatedRecords()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        credentialBroker.GetAccessTokenAsync(Arg.Any<AuthorizationTicket>(), "Bigin", "default", Arg.Any<CancellationToken>())
            .Returns(new AccessToken("mock_token_123", DateTimeOffset.UtcNow.AddHours(1)));

        var useCase = new GetBiginRelatedRecordsUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_viewer")
            .WithRoles(RoleCatalog.ViewerRole)
            .Build();

        var ticket = new AuthorizationTicket(
            "bigin.relatedlists.read",
            "fp_test",
            "Bigin",
            "default",
            DateTimeOffset.UtcNow.AddMinutes(5));

        var context = new UseCaseContext(principal, ticket, "corr_123");
        var command = new GetBiginRelatedRecordsCommand(
            "Contacts",
            "2034020000000478028",
            "Deals",
            new[] { "Deal_Name", "Stage" },
            1,
            200,
            "US");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Records.Count.ShouldBe(2);
        result.Value.Page.ShouldBe(1);
        result.Value.PerPage.ShouldBe(200);

        await credentialBroker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "123", "Deals", "INVALID_MODULE")]
    [InlineData("Contacts", "", "Deals", "INVALID_RECORD_ID")]
    [InlineData("Contacts", "123", "", "INVALID_RELATED_LIST")]
    [InlineData("Contacts", "123", "Deals", "INVALID_PAGE", 0)]
    [InlineData("Contacts", "123", "Deals", "INVALID_PER_PAGE", 1, 250)]
    public async Task HandleAsync_WithInvalidParameters_ReturnsFailure(
        string module,
        string recordId,
        string relatedList,
        string expectedError,
        int page = 1,
        int perPage = 200)
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        var useCase = new GetBiginRelatedRecordsUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_viewer")
            .WithRoles(RoleCatalog.ViewerRole)
            .Build();

        var ticket = new AuthorizationTicket(
            "bigin.relatedlists.read",
            "fp_test",
            "Bigin",
            "default",
            DateTimeOffset.UtcNow.AddMinutes(5));

        var context = new UseCaseContext(principal, ticket, "corr_123");
        var command = new GetBiginRelatedRecordsCommand(
            module,
            recordId,
            relatedList,
            null,
            page,
            perPage);

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(expectedError);
    }

    [Fact]
    public void GetBiginRelatedRecordsUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(GetBiginRelatedRecordsUseCase),
            typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.relatedlists.read");

        var perm = new Permission(attr.Permission);
        PermissionCatalog.Exists(perm).ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidDataCenter_ReturnsFailure()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        var useCase = new GetBiginRelatedRecordsUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_viewer").WithRoles(RoleCatalog.ViewerRole).Build();
        var ticket = new AuthorizationTicket("bigin.relatedlists.read", "fp_test", "Bigin", "default", DateTimeOffset.UtcNow.AddMinutes(5));
        var context = new UseCaseContext(principal, ticket, "corr_123");
        var command = new GetBiginRelatedRecordsCommand(
            "Contacts",
            "123",
            "Deals",
            null,
            1,
            200,
            "INVALID_DC");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_DATA_CENTER");
    }
}
