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
public class UploadBiginOrgPhotoUseCaseTests
{
    [Fact]
    public async Task HandleAsync_WithValidTicket_UploadsSuccessfully()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        credentialBroker.GetAccessTokenAsync(Arg.Any<AuthorizationTicket>(), "Bigin", "default", Arg.Any<CancellationToken>())
            .Returns(new AccessToken("mock_token_123", DateTimeOffset.UtcNow.AddHours(1)));

        var useCase = new UploadBiginOrgPhotoUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_operator")
            .WithRoles(RoleCatalog.OperatorRole)
            .Build();

        var ticket = new AuthorizationTicket(
            "bigin.org.write",
            "fp_test",
            "Bigin",
            "default",
            DateTimeOffset.UtcNow.AddMinutes(5));

        var context = new UseCaseContext(principal, ticket, "corr_123");
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        var command = new UploadBiginOrgPhotoCommand("US", imageBytes, "logo.png", "image/png");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Code.ShouldBe("SUCCESS");
        result.Value.Status.ShouldBe("success");

        await credentialBroker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithOversizedFile_ReturnsFailure()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        var useCase = new UploadBiginOrgPhotoUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_operator")
            .WithRoles(RoleCatalog.OperatorRole)
            .Build();

        var ticket = new AuthorizationTicket(
            "bigin.org.write",
            "fp_test",
            "Bigin",
            "default",
            DateTimeOffset.UtcNow.AddMinutes(5));

        var context = new UseCaseContext(principal, ticket, "corr_123");
        var oversizedBytes = new byte[(1024 * 1024) + 1];
        var command = new UploadBiginOrgPhotoCommand("US", oversizedBytes, "big.png", "image/png");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("FILE_SIZE_MORE_THAN_ALLOWED_SIZE");
        result.ErrorMessage.ShouldNotBeNull();
        result.ErrorMessage!.ShouldContain("exceeds allowed maximum limit of 1 MB");
    }

    [Fact]
    public async Task HandleAsync_WithEmptyFile_ReturnsFailure()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        var useCase = new UploadBiginOrgPhotoUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_operator")
            .WithRoles(RoleCatalog.OperatorRole)
            .Build();

        var ticket = new AuthorizationTicket(
            "bigin.org.write",
            "fp_test",
            "Bigin",
            "default",
            DateTimeOffset.UtcNow.AddMinutes(5));

        var context = new UseCaseContext(principal, ticket, "corr_123");
        var command = new UploadBiginOrgPhotoCommand("US", Array.Empty<byte>(), "logo.png", "image/png");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("EMPTY_FILE");
    }

    [Fact]
    public void UploadBiginOrgPhotoUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(UploadBiginOrgPhotoUseCase),
            typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.org.write");

        var perm = new Permission(attr.Permission);
        PermissionCatalog.Exists(perm).ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidDataCenter_ReturnsFailure()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        var useCase = new UploadBiginOrgPhotoUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_operator").WithRoles(RoleCatalog.OperatorRole).Build();
        var ticket = new AuthorizationTicket("bigin.org.write", "fp_test", "Bigin", "default", DateTimeOffset.UtcNow.AddMinutes(5));
        var context = new UseCaseContext(principal, ticket, "corr_123");
        var command = new UploadBiginOrgPhotoCommand("INVALID_DC", new byte[] { 1, 2, 3 }, "logo.png", "image/png");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_DATA_CENTER");
    }

    [Fact]
    public async Task HandleAsync_WithMissingExtension_ReturnsFailure()
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        var useCase = new UploadBiginOrgPhotoUseCase(credentialBroker);

        var principal = PrincipalBuilder.For("user_operator").WithRoles(RoleCatalog.OperatorRole).Build();
        var ticket = new AuthorizationTicket("bigin.org.write", "fp_test", "Bigin", "default", DateTimeOffset.UtcNow.AddMinutes(5));
        var context = new UseCaseContext(principal, ticket, "corr_123");
        var command = new UploadBiginOrgPhotoCommand("US", new byte[] { 1, 2, 3 }, "logo_without_extension", "image/png");

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_FILE_NAME");
    }
}
