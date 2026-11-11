using NSubstitute;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.UseCases;
using Pipeline.Application.UseCases.Bigin;
using Pipeline.Contracts.Requests;
using Pipeline.Domain.Security;
using Pipeline.TestKit.Builders;
using Shouldly;
using Xunit;

namespace Pipeline.Application.Tests.UseCases.Bigin;

[Trait("Category", "Unit")]
public class BiginNotificationUseCasesTests
{
    private static (UseCaseContext Context, ICredentialBroker CredentialBroker, AuthorizationTicket Ticket) CreateContext(string operationName)
    {
        var credentialBroker = Substitute.For<ICredentialBroker>();
        credentialBroker.GetAccessTokenAsync(Arg.Any<AuthorizationTicket>(), "Bigin", "default", Arg.Any<CancellationToken>())
            .Returns(new AccessToken("mock_bigin_token", DateTimeOffset.UtcNow.AddHours(1)));

        var principal = PrincipalBuilder.For("user_operator")
            .WithRoles(RoleCatalog.OperatorRole)
            .Build();

        var ticket = new AuthorizationTicket(
            operationName,
            "fp_notification_test",
            "Bigin",
            "default",
            DateTimeOffset.UtcNow.AddMinutes(5));

        var context = new UseCaseContext(principal, ticket, "corr_notify_123");
        return (context, credentialBroker, ticket);
    }

    [Fact]
    public async Task EnableBiginNotificationsUseCase_WithValidRequest_ReturnsSuccess()
    {
        var (context, broker, ticket) = CreateContext("bigin.notifications.write");
        var useCase = new EnableBiginNotificationsUseCase(broker);

        var channels = new List<BiginChannelSubscription>
        {
            new("1000000068001", new[] { "Contacts.create", "Contacts.edit" }, "https://pipeline.internal/webhook/bigin", "sample_token", "2026-11-01T00:00:00Z")
        };
        var command = new EnableBiginNotificationsCommand("US", channels);

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Watch.Count.ShouldBe(1);
        result.Value!.Watch[0].Code.ShouldBe("SUCCESS");
        result.Value!.Watch[0].ChannelId.ShouldBe("1000000068001");
        result.Value!.Watch[0].Events.ShouldNotBeNull();
        result.Value!.Watch[0].Events!.Count.ShouldBe(2);

        await broker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnableBiginNotificationsUseCase_WithInvalidDataCenter_ReturnsFailure()
    {
        var (context, broker, _) = CreateContext("bigin.notifications.write");
        var useCase = new EnableBiginNotificationsUseCase(broker);

        var channels = new List<BiginChannelSubscription>
        {
            new("1000000068001", new[] { "Contacts.create" }, "https://pipeline.internal/webhook/bigin")
        };
        var command = new EnableBiginNotificationsCommand("INVALID_DC", channels);

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_DATA_CENTER");
    }

    [Fact]
    public async Task EnableBiginNotificationsUseCase_WithEmptyChannels_ReturnsFailure()
    {
        var (context, broker, _) = CreateContext("bigin.notifications.write");
        var useCase = new EnableBiginNotificationsUseCase(broker);

        var command = new EnableBiginNotificationsCommand("US", new List<BiginChannelSubscription>());

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_CHANNELS");
    }

    [Fact]
    public void EnableBiginNotificationsUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(EnableBiginNotificationsUseCase), typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.notifications.write");
    }

    [Fact]
    public async Task GetBiginNotificationDetailsUseCase_WithValidRequest_ReturnsDetails()
    {
        var (context, broker, ticket) = CreateContext("bigin.notifications.read");
        var useCase = new GetBiginNotificationDetailsUseCase(broker);

        var command = new GetBiginNotificationDetailsCommand("US", "1000000068001", "Contacts", 1, 10);

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Watch.Count.ShouldBe(1);
        result.Value!.Watch[0].ChannelId.ShouldBe("1000000068001");
        result.Value!.Watch[0].ResourceName.ShouldBe("Contacts");
        result.Value!.Info.Page.ShouldBe(1);
        result.Value!.Info.PerPage.ShouldBe(10);

        await broker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBiginNotificationDetailsUseCase_WithInvalidPage_ReturnsFailure()
    {
        var (context, broker, _) = CreateContext("bigin.notifications.read");
        var useCase = new GetBiginNotificationDetailsUseCase(broker);

        var command = new GetBiginNotificationDetailsCommand("US", Page: 0);

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_PAGE");
    }

    [Fact]
    public void GetBiginNotificationDetailsUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(GetBiginNotificationDetailsUseCase), typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.notifications.read");
    }

    [Fact]
    public async Task UpdateBiginNotificationsUseCase_WithValidRequest_ReturnsSuccess()
    {
        var (context, broker, ticket) = CreateContext("bigin.notifications.write");
        var useCase = new UpdateBiginNotificationsUseCase(broker);

        var channels = new List<BiginChannelSubscription>
        {
            new("1000000068001", new[] { "Contacts.all" }, "https://pipeline.internal/webhook/bigin_updated")
        };
        var command = new UpdateBiginNotificationsCommand("US", channels);

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Watch.Count.ShouldBe(1);
        result.Value!.Watch[0].Message.ShouldBe("Notification updated successfully.");

        await broker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UpdateBiginNotificationsUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(UpdateBiginNotificationsUseCase), typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.notifications.write");
    }

    [Fact]
    public async Task UpdateSpecificBiginNotificationsUseCase_WithValidRequest_ReturnsSuccess()
    {
        var (context, broker, ticket) = CreateContext("bigin.notifications.write");
        var useCase = new UpdateSpecificBiginNotificationsUseCase(broker);

        var channels = new List<BiginChannelSubscription>
        {
            new("1000000068001", new[] { "Contacts.delete" }, "https://pipeline.internal/webhook/bigin")
        };
        var command = new UpdateSpecificBiginNotificationsCommand("US", channels);

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Watch.Count.ShouldBe(1);
        result.Value!.Watch[0].Message.ShouldBe("Specific notification details updated successfully.");

        await broker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UpdateSpecificBiginNotificationsUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(UpdateSpecificBiginNotificationsUseCase), typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.notifications.write");
    }

    [Fact]
    public async Task DisableBiginNotificationsUseCase_WithValidRequest_ReturnsSuccess()
    {
        var (context, broker, ticket) = CreateContext("bigin.notifications.write");
        var useCase = new DisableBiginNotificationsUseCase(broker);

        var command = new DisableBiginNotificationsCommand("US", new[] { "1000000068001", "1000000068002" });

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Watch.Count.ShouldBe(2);
        result.Value!.Watch[0].ChannelId.ShouldBe("1000000068001");
        result.Value!.Watch[0].Message.ShouldBe("Notification disabled successfully.");

        await broker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableBiginNotificationsUseCase_WithEmptyChannelIds_ReturnsFailure()
    {
        var (context, broker, _) = CreateContext("bigin.notifications.write");
        var useCase = new DisableBiginNotificationsUseCase(broker);

        var command = new DisableBiginNotificationsCommand("US", new List<string>());

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("INVALID_CHANNEL_IDS");
    }

    [Fact]
    public void DisableBiginNotificationsUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(DisableBiginNotificationsUseCase), typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.notifications.write");
    }

    [Fact]
    public async Task DisableSpecificBiginNotificationsUseCase_WithValidRequest_ReturnsSuccess()
    {
        var (context, broker, ticket) = CreateContext("bigin.notifications.write");
        var useCase = new DisableSpecificBiginNotificationsUseCase(broker);

        var channels = new List<BiginChannelSubscription>
        {
            new("1000000068001", new[] { "Contacts.create" }, null, DeleteEvents: true)
        };
        var command = new DisableSpecificBiginNotificationsCommand("US", channels);

        var result = await useCase.HandleAsync(command, context, CancellationToken.None);

        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Watch.Count.ShouldBe(1);
        result.Value!.Watch[0].Message.ShouldBe("Specific notifications disabled successfully.");

        await broker.Received(1).GetAccessTokenAsync(ticket, "Bigin", "default", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void DisableSpecificBiginNotificationsUseCase_DeclaresMandatoryRequiresPermissionAttribute()
    {
        var attr = (RequiresPermissionAttribute?)Attribute.GetCustomAttribute(
            typeof(DisableSpecificBiginNotificationsUseCase), typeof(RequiresPermissionAttribute));

        attr.ShouldNotBeNull();
        attr.Permission.ShouldBe("bigin.notifications.write");
    }
}
