using System.Text.Json;
using Pipeline.Contracts.Requests;
using Pipeline.Contracts.Responses;
using Shouldly;
using Xunit;

namespace Pipeline.Contract.Tests.Api;

[Trait("Category", "Contract")]
public class BiginNotificationContractTests
{
    [Fact]
    public void EnableBiginNotificationsRequest_RoundtripsCorrectly()
    {
        var request = new EnableBiginNotificationsRequest(
            DataCenter: "US",
            Channels: new List<BiginChannelSubscription>
            {
                new("1001", new[] { "Contacts.create" }, "https://example.com/callback", "TOKEN_1001", "2023-10-02T10:30:00+05:30")
            });

        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<EnableBiginNotificationsRequest>(json);

        deserialized.ShouldNotBeNull();
        deserialized.DataCenter.ShouldBe("US");
        deserialized.Channels.Count.ShouldBe(1);
        deserialized.Channels[0].ChannelId.ShouldBe("1001");
        deserialized.Channels[0].Events.ShouldBe(new[] { "Contacts.create" });
    }

    [Fact]
    public void BiginWatchOperationResponse_RoundtripsCorrectly()
    {
        var response = new BiginWatchOperationResponse(
            Watch: new List<BiginWatchChannelResponse>
            {
                new(
                    Code: "SUCCESS",
                    Status: "success",
                    Message: "Successfully subscribed for actions-watch of the given module",
                    Events: new List<BiginEventSubscriptionResponse>
                    {
                        new("2023-09-14T20:46:13+05:30", "https://www.zohoapis.com/bigin/v2/Contacts", "5436046000000002179", "Contacts", "1001")
                    },
                    ChannelId: "1001",
                    ResourceUri: "https://www.zohoapis.com/bigin/v2/Contacts",
                    ResourceId: "5436046000000002179")
            });

        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<BiginWatchOperationResponse>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Watch.Count.ShouldBe(1);
        deserialized.Watch[0].Code.ShouldBe("SUCCESS");
        deserialized.Watch[0].Events![0].ResourceName.ShouldBe("Contacts");
    }

    [Fact]
    public void GetBiginNotificationDetailsRequest_RoundtripsCorrectly()
    {
        var request = new GetBiginNotificationDetailsRequest(
            DataCenter: "EU",
            ChannelId: "1002",
            Module: "Accounts",
            Page: 2,
            PerPage: 50);

        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<GetBiginNotificationDetailsRequest>(json);

        deserialized.ShouldNotBeNull();
        deserialized.DataCenter.ShouldBe("EU");
        deserialized.ChannelId.ShouldBe("1002");
        deserialized.Module.ShouldBe("Accounts");
        deserialized.Page.ShouldBe(2);
        deserialized.PerPage.ShouldBe(50);
    }

    [Fact]
    public void GetBiginNotificationDetailsResponseContract_RoundtripsCorrectly()
    {
        var response = new GetBiginNotificationDetailsResponseContract(
            Watch: new List<BiginNotificationDetailsContract>
            {
                new(
                    ChannelId: "1001",
                    ResourceName: "Contacts",
                    ResourceUri: "https://www.zohoapis.com/bigin/v2/Contacts",
                    NotifyUrl: "https://webhook.site/cb",
                    Events: new[] { "Contacts.create" },
                    ChannelExpiry: "2023-10-10T20:10:09+05:30",
                    Token: "TOKEN_1001")
            },
            Info: new BiginNotificationPageInfoContract(
                PerPage: 200,
                Count: 1,
                Page: 1,
                MoreRecords: false));

        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<GetBiginNotificationDetailsResponseContract>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Watch.Count.ShouldBe(1);
        deserialized.Watch[0].ChannelId.ShouldBe("1001");
        deserialized.Info.Count.ShouldBe(1);
        deserialized.Info.MoreRecords.ShouldBeFalse();
    }
}
