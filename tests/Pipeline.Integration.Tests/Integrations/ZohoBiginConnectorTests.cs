using System.Net;
using System.Text.Json;
using Integrations.Zoho.Bigin;
using Integrations.Zoho.Bigin.Models;
using Shouldly;
using Xunit;

namespace Pipeline.Integration.Tests.Integrations;

[Trait("Category", "Integration")]
public class ZohoBiginConnectorTests
{
    private const string SampleAccessToken = "1000.8cb99dxxxxxxxxxxxxx9be93.9b8xxxxxxxxxxxxxxxf";

    private sealed class MockHttpTransport : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedRequestBody { get; private set; }
        public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;
        public string ResponseContent { get; set; } = "{}";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            if (request.Content is not null)
            {
                CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            var response = new HttpResponseMessage(ResponseStatusCode)
            {
                Content = new StringContent(ResponseContent, System.Text.Encoding.UTF8, "application/json")
            };
            return response;
        }
    }

    [Theory]
    [InlineData(BiginDataCenter.US, "www.zohoapis.com")]
    [InlineData(BiginDataCenter.EU, "www.zohoapis.eu")]
    [InlineData(BiginDataCenter.AU, "www.zohoapis.com.au")]
    [InlineData(BiginDataCenter.IN, "www.zohoapis.in")]
    [InlineData(BiginDataCenter.CN, "www.zohoapis.com.cn")]
    [InlineData(BiginDataCenter.JP, "www.zohoapis.jp")]
    [InlineData(BiginDataCenter.SA, "www.zohoapis.sa")]
    [InlineData(BiginDataCenter.CA, "www.zohoapis.ca")]
    public async Task EnableNotifications_SendsPost_ToCorrectDcEndpoint_WithAuthHeader(BiginDataCenter dc, string expectedDomain)
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
              "watch": [
                {
                  "code": "SUCCESS",
                  "details": {
                    "events": [
                      {
                        "channel_expiry": "2023-09-14T20:46:13+05:30",
                        "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
                        "resource_id": "5436046000000002179",
                        "resource_name": "Contacts",
                        "channel_id": "1001"
                      }
                    ]
                  },
                  "message": "Successfully subscribed for actions-watch of the given module",
                  "status": "success"
                }
              ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));

        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.create" },
                    ChannelExpiry = "2023-10-02T10:30:00+05:30",
                    Token = "TOKEN_FOR_VERIFICATION_OF_1001",
                    NotifyUrl = "https://webhook.site/09fff50d-b355-42a9-9c2b-9e2147b2a5e5"
                }
            }
        };

        var response = await connector.EnableNotificationsAsync(dc, SampleAccessToken, request);

        // Verification
        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.Method.ShouldBe(HttpMethod.Post);
        transport.CapturedRequest.RequestUri!.Host.ShouldBe(expectedDomain);
        transport.CapturedRequest.RequestUri.AbsolutePath.ShouldBe("/bigin/v2/actions/watch");

        // Auth Header verification
        var authHeader = transport.CapturedRequest.Headers.GetValues("Authorization").FirstOrDefault();
        authHeader.ShouldBe($"Zoho-oauthtoken {SampleAccessToken}");

        // Payload verification
        transport.CapturedRequestBody.ShouldNotBeNull();
        transport.CapturedRequestBody.ShouldContain("\"channel_id\":\"1001\"");
        transport.CapturedRequestBody.ShouldContain("\"Contacts.create\"");

        // Response verification
        response.Watch.Count.ShouldBe(1);
        response.AllSucceeded.ShouldBeTrue();
        var item = response.Watch[0];
        item.Code.ShouldBe("SUCCESS");
        item.Status.ShouldBe("success");
        item.Details.ShouldNotBeNull();
        item.Details!.Events.ShouldNotBeNull();
        item.Details.Events!.Count.ShouldBe(1);
        item.Details.Events[0].ResourceName.ShouldBe("Contacts");
        item.Details.Events[0].ResourceId.ShouldBe("5436046000000002179");
    }

    [Fact]
    public async Task GetNotificationDetails_WithFilter_SendsGet_WithQueryParameters()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
              "watch": [
                {
                  "notify_on_related_action": false,
                  "channel_expiry": "2023-10-10T20:10:09+05:30",
                  "return_affected_field_values": false,
                  "resource_uri": "https://www.zohoapis.com/bigin/v2/Pipelines",
                  "resource_id": "5436046000000002181",
                  "notify_url": "https://webhook.site/924ad5dc-e982-4525-894a-a1851507b1ae",
                  "resource_name": "Pipelines",
                  "fields": null,
                  "channel_id": "1002",
                  "events": [
                    "Pipelines.create"
                  ],
                  "token": "TOKEN_FOR_VERIFICATION_OF_1002"
                }
              ],
              "info": {
                "per_page": 200,
                "count": 1,
                "page": 1,
                "more_records": false
              }
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var query = new BiginNotificationDetailsQuery
        {
            ChannelId = "1002",
            Module = "Pipelines",
            Page = 1,
            PerPage = 200
        };

        var response = await connector.GetNotificationDetailsAsync(BiginDataCenter.US, SampleAccessToken, query);

        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.Method.ShouldBe(HttpMethod.Get);
        transport.CapturedRequest.RequestUri!.Query.ShouldContain("channel_id=1002");
        transport.CapturedRequest.RequestUri.Query.ShouldContain("module=Pipelines");
        transport.CapturedRequest.RequestUri.Query.ShouldContain("page=1");
        transport.CapturedRequest.RequestUri.Query.ShouldContain("per_page=200");

        response.Watch.Count.ShouldBe(1);
        response.Watch[0].ChannelId.ShouldBe("1002");
        response.Watch[0].ResourceName.ShouldBe("Pipelines");
        response.Info.Count.ShouldBe(1);
        response.Info.PerPage.ShouldBe(200);
        response.Info.MoreRecords.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateNotificationDetails_SendsPut_OverwritesChannel()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
              "watch": [
                {
                  "code": "SUCCESS",
                  "details": {
                    "events": [
                      {
                        "channel_expiry": "2023-10-12T14:14:06+05:30",
                        "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
                        "resource_id": "5436046000000002179",
                        "resource_name": "Contacts",
                        "channel_id": "1001"
                      },
                      {
                        "channel_expiry": "2023-10-12T14:14:06+05:30",
                        "resource_uri": "https://www.zohoapis.com/bigin/v2/Accounts",
                        "resource_id": "5436046000000002177",
                        "resource_name": "Accounts",
                        "channel_id": "1001"
                      }
                    ]
                  },
                  "message": "Successfully subscribed for actions-watch of the given module",
                  "status": "success"
                }
              ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.edit", "Accounts.edit" },
                    Token = "UPDATED_TOKEN_FOR_VERIFICATION_OF_1001",
                    NotifyUrl = "https://webhook.site/924ad5dc-e982-4525-894a-a1851507b1ae"
                }
            }
        };

        var response = await connector.UpdateNotificationDetailsAsync(BiginDataCenter.EU, SampleAccessToken, request);

        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.Method.ShouldBe(HttpMethod.Put);
        transport.CapturedRequest.RequestUri!.Host.ShouldBe("www.zohoapis.eu");
        transport.CapturedRequestBody.ShouldNotBeNull();
        transport.CapturedRequestBody.ShouldContain("\"UPDATED_TOKEN_FOR_VERIFICATION_OF_1001\"");

        response.Watch.Count.ShouldBe(1);
        response.Watch[0].Details!.Events!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task UpdateSpecificNotificationDetails_SendsPatch_AppendsEvents()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
              "watch": [
                {
                  "code": "SUCCESS",
                  "details": {
                    "events": [
                      {
                        "channel_expiry": "2023-10-12T14:42:23+05:30",
                        "resource_uri": "https://www.zohoapis.com/bigin/v2/Pipelines",
                        "resource_id": "5436046000000002181",
                        "resource_name": "Pipelines",
                        "channel_id": "1001"
                      }
                    ]
                  },
                  "message": "Successfully updated the subscribe details",
                  "status": "success"
                }
              ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Pipelines.edit" },
                    ChannelExpiry = "2023-12-02T10:30:00+05:30",
                    Token = "UPDATE_SPECIFIC_TOKEN_FOR_VERIFICATION_OF_1002",
                    NotifyUrl = "https://webhook.site/924ad5dc-e982-4525-894a-a1851507b1ae"
                }
            }
        };

        var response = await connector.UpdateSpecificNotificationDetailsAsync(BiginDataCenter.AU, SampleAccessToken, request);

        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.Method.ShouldBe(HttpMethod.Patch);
        transport.CapturedRequest.RequestUri!.Host.ShouldBe("www.zohoapis.com.au");

        response.Watch[0].IsSuccess.ShouldBeTrue();
        response.Watch[0].Details!.Events![0].ResourceName.ShouldBe("Pipelines");
    }

    [Fact]
    public async Task DisableNotifications_SendsDelete_WithCommaSeparatedChannelIds()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
              "watch": [
                {
                  "code": "SUCCESS",
                  "details": {
                    "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
                    "resource_id": "5436046000000002179",
                    "channel_id": "1001"
                  },
                  "message": "Successfully un-subscribed from actions-watch",
                  "status": "success"
                },
                {
                  "code": "SUCCESS",
                  "details": {
                    "resource_uri": "https://www.zohoapis.com/bigin/v2/Pipelines",
                    "resource_id": "5436046000000002181",
                    "channel_id": "1002"
                  },
                  "message": "Successfully un-subscribed from actions-watch",
                  "status": "success"
                }
              ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var response = await connector.DisableNotificationsAsync(BiginDataCenter.IN, SampleAccessToken, new[] { "1001", "1002" });

        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.Method.ShouldBe(HttpMethod.Delete);
        transport.CapturedRequest.RequestUri!.Host.ShouldBe("www.zohoapis.in");
        transport.CapturedRequest.RequestUri.Query.ShouldContain("channel_ids=1001%2C1002");

        response.Watch.Count.ShouldBe(2);
        response.Watch[0].Details!.ChannelId.ShouldBe("1001");
        response.Watch[1].Details!.ChannelId.ShouldBe("1002");
    }

    [Fact]
    public async Task DisableSpecificNotifications_SendsPatch_WithDeleteEventsTrue()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
              "watch": [
                {
                  "code": "SUCCESS",
                  "details": {
                    "events": [
                      {
                        "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
                        "resource_id": "5436046000000002179",
                        "resource_name": "Contacts",
                        "channel_id": "1001"
                      },
                      {
                        "resource_uri": "https://www.zohoapis.com/bigin/v2/Pipelines",
                        "resource_id": "5436046000000002181",
                        "resource_name": "Pipelines",
                        "channel_id": "1001"
                      }
                    ]
                  },
                  "message": "Successfully removed the subscribe details",
                  "status": "success"
                }
              ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.edit", "Pipelines.edit" },
                    DeleteEvents = true
                }
            }
        };

        var response = await connector.DisableSpecificNotificationsAsync(BiginDataCenter.JP, SampleAccessToken, request);

        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.Method.ShouldBe(HttpMethod.Patch);
        transport.CapturedRequest.RequestUri!.Host.ShouldBe("www.zohoapis.jp");
        transport.CapturedRequestBody.ShouldNotBeNull();
        transport.CapturedRequestBody.ShouldContain("\"_delete_events\":true");

        response.Watch[0].Details!.Events!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task EnableNotifications_EnforcesMax50CharToken()
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));
        var oversizedToken = new string('A', 51);

        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.create" },
                    Token = oversizedToken,
                    NotifyUrl = "https://example.com/callback"
                }
            }
        };

        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.EnableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);
        });

        ex.Message.ShouldContain("Token must not exceed 50 characters");
    }

    [Fact]
    public async Task EnableNotifications_EnforcesMax100Channels()
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));

        var channels = Enumerable.Range(1, 101).Select(i => new BiginWatchChannelRequest
        {
            ChannelId = $"ch_{i}",
            Events = new[] { "Contacts.create" },
            NotifyUrl = "https://example.com/callback"
        }).ToList();

        var request = new BiginWatchRequestBody { Watch = channels };

        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.EnableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);
        });

        ex.Message.ShouldContain("Cannot configure more than 100 channels");
    }

    [Fact]
    public async Task ApiError_Http202_ThrowsBiginApiException_WithErrorCode()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.Accepted, // 202
            ResponseContent = """
            {
              "watch": [
                {
                  "code": "MANDATORY_NOT_FOUND",
                  "details": {
                    "api_name": "events"
                  },
                  "message": "One of the mandatory fields is missing",
                  "status": "error"
                }
              ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.create" },
                    NotifyUrl = "https://example.com/callback"
                }
            }
        };

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.EnableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        ex.BiginErrorCode.ShouldBe("MANDATORY_NOT_FOUND");
        ex.BiginStatus.ShouldBe("error");
    }

    [Fact]
    public async Task ApiError_Http401_ThrowsBiginApiException()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.Unauthorized,
            ResponseContent = """
            {
              "code": "INVALID_TOKEN",
              "message": "Invalid OAuth token provided.",
              "status": "error"
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.GetNotificationDetailsAsync(BiginDataCenter.US, SampleAccessToken);
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ex.BiginErrorCode.ShouldBe("INVALID_TOKEN");
    }

    [Fact]
    public async Task GetNotificationDetails_WithCompaniesModule_NormalizesToAccountsQueryParameter()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
              "watch": [],
              "info": {
                "per_page": 200,
                "count": 0,
                "page": 1,
                "more_records": false
              }
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var query = new BiginNotificationDetailsQuery
        {
            Module = "Companies"
        };

        var response = await connector.GetNotificationDetailsAsync(BiginDataCenter.US, SampleAccessToken, query);

        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.RequestUri!.Query.ShouldContain("module=Accounts");
        response.Watch.ShouldBeEmpty();
    }

    [Fact]
    public async Task EnableNotifications_WithInvalidExpiry_ThrowsArgumentException()
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));

        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.create" },
                    ChannelExpiry = "invalid-date",
                    NotifyUrl = "https://example.com/callback"
                }
            }
        };

        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.EnableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);
        });

        ex.Message.ShouldContain("channel_expiry must be a valid ISO8601");
    }

    [Fact]
    public async Task EnableNotifications_WithEmptyEvent_ThrowsArgumentException()
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));

        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "  " },
                    NotifyUrl = "https://example.com/callback"
                }
            }
        };

        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.EnableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);
        });

        ex.Message.ShouldContain("At least one non-empty event is required");
    }

    [Fact]
    public async Task DisableNotifications_Exceeding100Channels_ThrowsArgumentException()
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));
        var channelIds = Enumerable.Range(1, 101).Select(i => $"channel_{i}").ToList();

        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.DisableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, channelIds);
        });

        ex.Message.ShouldContain("Cannot disable more than 100 channels");
    }

    [Theory]
    [InlineData(0, 50)]     // invalid page < 1
    [InlineData(-1, 50)]    // invalid negative page
    [InlineData(1, 0)]      // invalid per_page < 1
    [InlineData(1, 201)]    // invalid per_page > 200 (max allowed is 200)
    public async Task GetNotificationDetails_ValidatesPageAndPerPageLimits(int page, int perPage)
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));
        var query = new BiginNotificationDetailsQuery
        {
            Page = page,
            PerPage = perPage
        };

        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
        {
            await connector.GetNotificationDetailsAsync(BiginDataCenter.US, SampleAccessToken, query);
        });
    }

    [Fact]
    public async Task GetNotificationDetails_UnknownModule_ThrowsArgumentException()
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));
        var query = new BiginNotificationDetailsQuery
        {
            Module = "UnknownCustomModule"
        };

        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.GetNotificationDetailsAsync(BiginDataCenter.US, SampleAccessToken, query);
        });

        ex.Message.ShouldContain("Unknown Bigin module 'UnknownCustomModule'");
    }

    [Fact]
    public async Task GetNotificationDetails_Status401_ThrowsBiginApiException()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.Unauthorized,
            ResponseContent = """
            {
              "code": "INVALID_TOKEN",
              "message": "invalid oauth token",
              "status": "error"
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.GetNotificationDetailsAsync(BiginDataCenter.US, SampleAccessToken);
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ex.BiginErrorCode.ShouldBe("INVALID_TOKEN");
    }

    [Fact]
    public async Task DisableNotifications_EmptyChannelIds_ThrowsArgumentException()
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));

        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.DisableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, Array.Empty<string>());
        });

        ex.Message.ShouldContain("At least one valid channel_id is required");
    }

    [Fact]
    public async Task EnableNotifications_EmptyChannels_ThrowsArgumentException()
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));
        var request = new BiginWatchRequestBody
        {
            Watch = Array.Empty<BiginWatchChannelRequest>()
        };

        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.EnableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);
        });

        ex.Message.ShouldContain("Watch channels list must contain at least one channel");
    }

    [Theory]
    [InlineData("not-a-valid-url")]
    [InlineData("ftp://example.com/callback")]
    [InlineData("")]
    public async Task EnableNotifications_InvalidNotifyUrl_ThrowsArgumentException(string url)
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));
        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.create" },
                    NotifyUrl = url
                }
            }
        };

        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.EnableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);
        });
    }

    [Fact]
    public void BiginNotificationScopes_ValidatesExpectedScopes()
    {
        BiginNotificationScopes.IsValidEnableScope(BiginNotificationScopes.All).ShouldBeTrue();
        BiginNotificationScopes.IsValidEnableScope(BiginNotificationScopes.Write).ShouldBeTrue();
        BiginNotificationScopes.IsValidEnableScope(BiginNotificationScopes.Create).ShouldBeTrue();
        BiginNotificationScopes.IsValidEnableScope(BiginNotificationScopes.Read).ShouldBeFalse();

        BiginNotificationScopes.IsValidReadScope(BiginNotificationScopes.Read).ShouldBeTrue();
        BiginNotificationScopes.IsValidReadScope(BiginNotificationScopes.All).ShouldBeTrue();
        BiginNotificationScopes.IsValidReadScope(BiginNotificationScopes.Write).ShouldBeFalse();

        BiginNotificationScopes.IsValidUpdateScope(BiginNotificationScopes.Update).ShouldBeTrue();
        BiginNotificationScopes.IsValidUpdateScope(BiginNotificationScopes.All).ShouldBeTrue();

        BiginNotificationScopes.IsValidDeleteScope(BiginNotificationScopes.Delete).ShouldBeTrue();
        BiginNotificationScopes.IsValidDeleteScope(BiginNotificationScopes.All).ShouldBeTrue();
        BiginNotificationScopes.IsValidDeleteScope(BiginNotificationScopes.Create).ShouldBeFalse();
    }

    [Fact]
    public async Task DisableSpecificNotifications_WithNullDeleteEvents_DefaultsToTrue()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
              "watch": [
                {
                  "code": "SUCCESS",
                  "details": {
                    "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
                    "resource_id": "5436046000000002179",
                    "resource_name": "Contacts",
                    "channel_id": "1001"
                  },
                  "message": "Successfully removed the subscribe details",
                  "status": "success"
                }
              ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.create" }
                    // DeleteEvents is intentionally omitted (null)
                }
            }
        };

        var response = await connector.DisableSpecificNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);

        transport.CapturedRequestBody.ShouldNotBeNull();
        transport.CapturedRequestBody.ShouldContain("\"_delete_events\":true");
        response.AllSucceeded.ShouldBeTrue();
    }

    [Fact]
    public async Task BatchWatch_PartialFailures_ReturnsBatchWithIndividualStatuses()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
              "watch": [
                {
                  "code": "SUCCESS",
                  "status": "success",
                  "message": "Successfully subscribed for actions-watch of the given module",
                  "details": {
                    "events": [
                      {
                        "channel_expiry": "2023-09-14T20:46:13+05:30",
                        "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
                        "resource_id": "5436046000000002179",
                        "resource_name": "Contacts",
                        "channel_id": "1001"
                      }
                    ]
                  }
                },
                {
                  "code": "INVALID_DATA",
                  "status": "error",
                  "message": "The length of the token is greater than 50 characters.",
                  "details": {
                    "api_name": "token"
                  }
                }
              ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.create" },
                    NotifyUrl = "https://example.com/callback"
                },
                new()
                {
                    ChannelId = "1002",
                    Events = new[] { "Pipelines.create" },
                    NotifyUrl = "https://example.com/callback"
                }
            }
        };

        var response = await connector.EnableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);

        response.Watch.Count.ShouldBe(2);
        response.AllSucceeded.ShouldBeFalse();
        response.Watch[0].IsSuccess.ShouldBeTrue();
        response.Watch[1].IsSuccess.ShouldBeFalse();
        response.Watch[1].Code.ShouldBe("INVALID_DATA");
        response.Watch[1].Details.ShouldNotBeNull();
        response.Watch[1].Details!.ApiName.ShouldBe("token");
    }

    [Fact]
    public async Task BatchWatch_AllFailures_ThrowsBiginApiException_WithDetailsAndMissingApiName()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.Accepted,
            ResponseContent = """
            {
              "watch": [
                {
                  "code": "MANDATORY_NOT_FOUND",
                  "status": "error",
                  "message": "One of the mandatory fields (events, channel_id, notify_url) is not found.",
                  "details": {
                    "api_name": "notify_url"
                  }
                }
              ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var request = new BiginWatchRequestBody
        {
            Watch = new List<BiginWatchChannelRequest>
            {
                new()
                {
                    ChannelId = "1001",
                    Events = new[] { "Contacts.create" },
                    NotifyUrl = "https://example.com/callback"
                }
            }
        };

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.EnableNotificationsAsync(BiginDataCenter.US, SampleAccessToken, request);
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        ex.BiginErrorCode.ShouldBe("MANDATORY_NOT_FOUND");
        ex.MissingApiName.ShouldBe("notify_url");
        ex.Details.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(BiginDataCenter.US, "www.zohoapis.com")]
    [InlineData(BiginDataCenter.EU, "www.zohoapis.eu")]
    [InlineData(BiginDataCenter.AU, "www.zohoapis.com.au")]
    [InlineData(BiginDataCenter.IN, "www.zohoapis.in")]
    [InlineData(BiginDataCenter.CN, "www.zohoapis.com.cn")]
    [InlineData(BiginDataCenter.JP, "www.zohoapis.jp")]
    [InlineData(BiginDataCenter.SA, "www.zohoapis.sa")]
    [InlineData(BiginDataCenter.CA, "www.zohoapis.ca")]
    public async Task GetOrgDetails_SendsGet_ToCorrectDcEndpoint_WithAuthHeader(BiginDataCenter dc, string expectedDomain)
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
                "org": [
                    {
                        "country": "United States",
                        "city": "Anytown",
                        "street": "123 Main Street",
                        "state": "NY",
                        "zip": "12345",
                        "id": "2034020000000013208",
                        "company_name": "Zylker Travels",
                        "primary_email": "travels@zylker.com",
                        "phone": "+1 (123) 456-7890",
                        "currency": "US Dollar",
                        "currency_symbol": "$",
                        "currency_locale": "USD",
                        "iso_code": "USD",
                        "time_zone": "Asia/Kolkata",
                        "domain_name": "org75407139",
                        "zgid": "75407139",
                        "country_code": "US",
                        "mc_status": false,
                        "gapps_enabled": false,
                        "lite_users_enabled": false,
                        "translation_enabled": false,
                        "deletable_org_account": false,
                        "privacy_settings": false,
                        "hipaa_compliance_enabled": false,
                        "hierarchy_preferences": {
                            "type": "Role_Hierarchy"
                        },
                        "license_details": {
                            "paid_expiry": "2024-04-11T05:30:00+05:30",
                            "users_license_purchased": 5,
                            "trial_type": null,
                            "trial_expiry": null,
                            "paid": true,
                            "paid_type": "biginpremier"
                        }
                    }
                ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var response = await connector.GetOrgDetailsAsync(dc, SampleAccessToken);

        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.Method.ShouldBe(HttpMethod.Get);
        transport.CapturedRequest.RequestUri!.Host.ShouldBe(expectedDomain);
        transport.CapturedRequest.RequestUri.AbsolutePath.ShouldBe("/bigin/v2/org");

        var authHeader = transport.CapturedRequest.Headers.GetValues("Authorization").FirstOrDefault();
        authHeader.ShouldBe($"Zoho-oauthtoken {SampleAccessToken}");

        response.Org.Count.ShouldBe(1);
        var org = response.Org[0];
        org.Id.ShouldBe("2034020000000013208");
        org.CompanyName.ShouldBe("Zylker Travels");
        org.Country.ShouldBe("United States");
        org.City.ShouldBe("Anytown");
        org.HierarchyPreferences.ShouldNotBeNull();
        org.HierarchyPreferences!.Type.ShouldBe("Role_Hierarchy");
        org.LicenseDetails.ShouldNotBeNull();
        org.LicenseDetails!.Paid.ShouldBe(true);
        org.LicenseDetails.PaidType.ShouldBe("biginpremier");
        org.LicenseDetails.UsersLicensePurchased.ShouldBe(5);
    }

    [Fact]
    public async Task UploadOrgPhoto_SendsMultipartFormData_WithFileAndAuthHeader()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
                "code": "SUCCESS",
                "details": {},
                "message": "photo uploaded successfully",
                "status": "success"
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG magic bytes

        var response = await connector.UploadOrgPhotoAsync(
            BiginDataCenter.EU,
            SampleAccessToken,
            imageBytes,
            "logo.png",
            "image/png");

        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.Method.ShouldBe(HttpMethod.Post);
        transport.CapturedRequest.RequestUri!.Host.ShouldBe("www.zohoapis.eu");
        transport.CapturedRequest.RequestUri.AbsolutePath.ShouldBe("/bigin/v2/org/photo");

        var authHeader = transport.CapturedRequest.Headers.GetValues("Authorization").FirstOrDefault();
        authHeader.ShouldBe($"Zoho-oauthtoken {SampleAccessToken}");

        transport.CapturedRequest.Content.ShouldBeOfType<MultipartFormDataContent>();
        response.IsSuccess.ShouldBeTrue();
        response.Code.ShouldBe("SUCCESS");
        response.Message.ShouldBe("photo uploaded successfully");
    }

    [Fact]
    public async Task UploadOrgPhoto_OversizedFile_ThrowsArgumentException_BeforeNetwork()
    {
        var transport = new MockHttpTransport();
        var connector = new BiginConnector(new HttpClient(transport));

        // 1 MB + 1 byte
        var oversizedBytes = new byte[(1024 * 1024) + 1];

        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.UploadOrgPhotoAsync(
                BiginDataCenter.US,
                SampleAccessToken,
                oversizedBytes,
                "big.png");
        });

        ex.Message.ShouldContain("exceeds the allowed limit of 1 MB");
        transport.CapturedRequest.ShouldBeNull();
    }

    [Fact]
    public async Task UploadOrgPhoto_Status415_ThrowsBiginApiException_FileSizeMoreThanAllowedSize()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.UnsupportedMediaType,
            ResponseContent = """
            {
                "code": "FILE_SIZE_MORE_THAN_ALLOWED_SIZE",
                "message": "Image resolution is more than allowed limit 10 MP, please upload after resizing image dimension or choose other image",
                "status": "error"
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var imageBytes = new byte[] { 0x1, 0x2, 0x3 };

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.UploadOrgPhotoAsync(
                BiginDataCenter.US,
                SampleAccessToken,
                imageBytes,
                "logo.png");
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        ex.BiginErrorCode.ShouldBe(BiginErrorCodes.FileSizeMoreThanAllowedSize);
        ex.BiginStatus.ShouldBe("error");
    }

    [Fact]
    public async Task GetRelatedListRecords_SendsGet_WithQueryParameters()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
                "data": [
                    {
                        "Contact_Role": null,
                        "Deal_Name": "Wildlife Safari Package",
                        "Stage": "Proposal/Price Quote",
                        "id": "2034020000000478276",
                        "Sub_Pipeline": "Sales Pipeline Standard",
                        "Pipeline": {
                            "name": "Sales - Marketing",
                            "id": "2034020000000095023"
                        }
                    },
                    {
                        "Contact_Role": null,
                        "Deal_Name": "City Tour Package",
                        "Stage": "Qualification",
                        "id": "2034020000000478136",
                        "Sub_Pipeline": "Sales Pipeline Standard",
                        "Pipeline": {
                            "name": "Sales - Marketing",
                            "id": "2034020000000095023"
                        }
                    }
                ],
                "info": {
                    "per_page": 200,
                    "next_page_token": null,
                    "count": 2,
                    "page": 1,
                    "previous_page_token": null,
                    "page_token_expiry": null,
                    "more_records": false
                }
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var query = new BiginRelatedListQuery
        {
            Fields = new[] { "Deal_Name", "Sub_Pipeline", "Stage", "Pipeline" },
            Page = 1,
            PerPage = 200
        };

        var response = await connector.GetRelatedListRecordsAsync(
            BiginDataCenter.AU,
            SampleAccessToken,
            "Contacts",
            "2034020000000478028",
            "Deals",
            query);

        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.Method.ShouldBe(HttpMethod.Get);
        transport.CapturedRequest.RequestUri!.Host.ShouldBe("www.zohoapis.com.au");
        transport.CapturedRequest.RequestUri.AbsolutePath.ShouldBe("/bigin/v2/Contacts/2034020000000478028/Deals");
        transport.CapturedRequest.RequestUri.Query.ShouldContain("fields=Deal_Name%2CSub_Pipeline%2CStage%2CPipeline");
        transport.CapturedRequest.RequestUri.Query.ShouldContain("page=1");
        transport.CapturedRequest.RequestUri.Query.ShouldContain("per_page=200");

        response.Data.ShouldNotBeNull();
        response.Data!.Count.ShouldBe(2);
        response.Info.ShouldNotBeNull();
        response.Info!.Count.ShouldBe(2);
        response.Info.PerPage.ShouldBe(200);
        response.Info.Page.ShouldBe(1);
        response.Info.MoreRecords.ShouldBe(false);
    }

    [Fact]
    public async Task GetRelatedListRecords_EmailsList_OmitsFieldsParameter()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
                "data": [],
                "info": {
                    "per_page": 200,
                    "count": 0,
                    "page": 1,
                    "more_records": false
                }
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var query = new BiginRelatedListQuery
        {
            Fields = new[] { "Subject", "From", "To" },
            Page = 1,
            PerPage = 50
        };

        var response = await connector.GetRelatedListRecordsAsync(
            BiginDataCenter.US,
            SampleAccessToken,
            "Contacts",
            "2034020000000478028",
            "Emails",
            query);

        transport.CapturedRequest.ShouldNotBeNull();
        // Documentation specifies: "The Emails related list currently does not support the fields query parameter."
        transport.CapturedRequest.RequestUri!.Query.ShouldNotContain("fields=");
        transport.CapturedRequest.RequestUri.Query.ShouldContain("page=1");
        transport.CapturedRequest.RequestUri.Query.ShouldContain("per_page=50");
    }

    [Theory]
    [InlineData(0, 100)]    // invalid page (page < 1)
    [InlineData(-1, 100)]   // invalid negative page
    [InlineData(1, 0)]      // invalid per_page < 1
    [InlineData(1, 201)]    // invalid per_page > 200 (max allowed is 200)
    public async Task GetRelatedListRecords_ValidatesPageAndPerPageLimits(int page, int perPage)
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));
        var query = new BiginRelatedListQuery
        {
            Page = page,
            PerPage = perPage
        };

        await Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
        {
            await connector.GetRelatedListRecordsAsync(
                BiginDataCenter.US,
                SampleAccessToken,
                "Contacts",
                "123",
                "Deals",
                query);
        });
    }

    [Fact]
    public async Task GetRelatedListRecords_Status401_ThrowsBiginApiException()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.Unauthorized,
            ResponseContent = """
            {
                "code": "OAUTH_SCOPE_MISMATCH",
                "message": "invalid oauth scope to access this URL",
                "status": "error"
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.GetRelatedListRecordsAsync(
                BiginDataCenter.IN,
                SampleAccessToken,
                "Contacts",
                "123",
                "Deals");
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ex.BiginErrorCode.ShouldBe(BiginErrorCodes.OAuthScopeMismatch);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetOrgDetails_ThrowsArgumentException_WhenAccessTokenIsNullOrWhitespace(string token)
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));

        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.GetOrgDetailsAsync(BiginDataCenter.US, token);
        });
    }

    [Fact]
    public async Task GetOrgDetails_FullDocumentationPayload_ParsesAll35FieldsCorrectly()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
                "org": [
                    {
                        "country": "United States",
                        "hierarchy_preferences": {
                            "type": "Role_Hierarchy"
                        },
                        "photo_id": "872175758e70545d3e5dcaxxxxbacc0fc9f30dcadbbeb4e4dd0c233701b6d361a6b72xxxx",
                        "city": "Anytown",
                        "description": "Zylker Travels is a full-service travel agency that specializes in creating unforgettable vacation experiences for our clients",
                        "mc_status": false,
                        "gapps_enabled": false,
                        "lite_users_enabled": false,
                        "domain_name": "org75407139",
                        "translation_enabled": false,
                        "street": "123 Main Street",
                        "alias": null,
                        "currency": "US Dollar",
                        "deletable_org_account": false,
                        "id": "2034020000000013208",
                        "state": "NY",
                        "fax": "+1 (123) 456-7899",
                        "employee_count": null,
                        "zip": "12345",
                        "website": "www.ztsample.com",
                        "currency_symbol": "$",
                        "mobile": "+1 (987) 654-3210",
                        "currency_locale": "USD",
                        "primary_zuid": "75407535",
                        "zia_portal_id": null,
                        "time_zone": "Asia/Kolkata",
                        "zgid": "75407139",
                        "country_code": "US",
                        "license_details": {
                            "paid_expiry": "2024-04-11T05:30:00+05:30",
                            "users_license_purchased": 5,
                            "trial_type": null,
                            "trial_expiry": null,
                            "paid": true,
                            "paid_type": "biginpremier"
                        },
                        "phone": "+1 (123) 456-7890",
                        "company_name": "Zylker Travels",
                        "privacy_settings": false,
                        "primary_email": "xxxxx.xxxxx@xxx.com",
                        "hipaa_compliance_enabled": false,
                        "iso_code": "USD"
                    }
                ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var response = await connector.GetOrgDetailsAsync(BiginDataCenter.US, SampleAccessToken);

        response.Org.Count.ShouldBe(1);
        var org = response.Org[0];
        org.Id.ShouldBe("2034020000000013208");
        org.Country.ShouldBe("United States");
        org.City.ShouldBe("Anytown");
        org.Street.ShouldBe("123 Main Street");
        org.State.ShouldBe("NY");
        org.Zip.ShouldBe("12345");
        org.PhotoId.ShouldBe("872175758e70545d3e5dcaxxxxbacc0fc9f30dcadbbeb4e4dd0c233701b6d361a6b72xxxx");
        org.Description.ShouldNotBeNull();
        org.Description!.ShouldContain("Zylker Travels is a full-service");
        org.McStatus.ShouldBe(false);
        org.GappsEnabled.ShouldBe(false);
        org.LiteUsersEnabled.ShouldBe(false);
        org.DomainName.ShouldBe("org75407139");
        org.TranslationEnabled.ShouldBe(false);
        org.Alias.ShouldBeNull();
        org.Currency.ShouldBe("US Dollar");
        org.DeletableOrgAccount.ShouldBe(false);
        org.Fax.ShouldBe("+1 (123) 456-7899");
        org.EmployeeCount.ShouldBeNull();
        org.Website.ShouldBe("www.ztsample.com");
        org.CurrencySymbol.ShouldBe("$");
        org.Mobile.ShouldBe("+1 (987) 654-3210");
        org.CurrencyLocale.ShouldBe("USD");
        org.PrimaryZuid.ShouldBe("75407535");
        org.ZiaPortalId.ShouldBeNull();
        org.TimeZone.ShouldBe("Asia/Kolkata");
        org.Zgid.ShouldBe("75407139");
        org.CountryCode.ShouldBe("US");
        org.Phone.ShouldBe("+1 (123) 456-7890");
        org.CompanyName.ShouldBe("Zylker Travels");
        org.PrivacySettings.ShouldBe(false);
        org.PrimaryEmail.ShouldBe("xxxxx.xxxxx@xxx.com");
        org.HipaaComplianceEnabled.ShouldBe(false);
        org.IsoCode.ShouldBe("USD");
        org.HierarchyPreferences!.Type.ShouldBe("Role_Hierarchy");
        org.LicenseDetails!.PaidExpiry.ShouldBe("2024-04-11T05:30:00+05:30");
        org.LicenseDetails.UsersLicensePurchased.ShouldBe(5);
        org.LicenseDetails.TrialType.ShouldBeNull();
        org.LicenseDetails.TrialExpiry.ShouldBeNull();
        org.LicenseDetails.Paid.ShouldBe(true);
        org.LicenseDetails.PaidType.ShouldBe("biginpremier");
    }

    [Fact]
    public async Task GetOrgDetails_WithNullableBooleans_HandlesNullsWithoutCrashing()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
                "org": [
                    {
                        "id": "2034020000000013208",
                        "company_name": "Test Org",
                        "mc_status": null,
                        "gapps_enabled": null,
                        "lite_users_enabled": null,
                        "translation_enabled": null,
                        "deletable_org_account": null,
                        "privacy_settings": null,
                        "hipaa_compliance_enabled": null,
                        "license_details": {
                            "paid": null,
                            "paid_type": null
                        }
                    }
                ]
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var response = await connector.GetOrgDetailsAsync(BiginDataCenter.US, SampleAccessToken);

        response.Org.Count.ShouldBe(1);
        var org = response.Org[0];
        org.McStatus.ShouldBeNull();
        org.IsMcStatus.ShouldBeFalse();
        org.PrivacySettings.ShouldBeNull();
        org.IsPrivacySettings.ShouldBeFalse();
        org.HipaaComplianceEnabled.ShouldBeNull();
        org.IsHipaaComplianceEnabled.ShouldBeFalse();
        org.LicenseDetails!.Paid.ShouldBeNull();
        org.LicenseDetails.IsPaid.ShouldBeFalse();
    }

    [Fact]
    public async Task UploadOrgPhoto_EmptyFile_ThrowsArgumentException()
    {
        var transport = new MockHttpTransport();
        var connector = new BiginConnector(new HttpClient(transport));

        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.UploadOrgPhotoAsync(
                BiginDataCenter.US,
                SampleAccessToken,
                Array.Empty<byte>(),
                "logo.png");
        });

        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            using var emptyStream = new MemoryStream();
            await connector.UploadOrgPhotoAsync(
                BiginDataCenter.US,
                SampleAccessToken,
                emptyStream,
                "logo.png");
        });
    }

    [Fact]
    public async Task UploadOrgPhoto_MissingExtension_ThrowsArgumentException()
    {
        var connector = new BiginConnector(new HttpClient(new MockHttpTransport()));

        await Should.ThrowAsync<ArgumentException>(async () =>
        {
            await connector.UploadOrgPhotoAsync(
                BiginDataCenter.US,
                SampleAccessToken,
                new byte[] { 1, 2, 3 },
                "logo_without_extension");
        });
    }

    private sealed class NonSeekableStream : Stream
    {
        private readonly Stream _inner;
        public NonSeekableStream(Stream inner) => _inner = inner;
        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    [Fact]
    public async Task UploadOrgPhoto_NonSeekableStream_StreamsAndHandlesStatus415()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.UnsupportedMediaType,
            ResponseContent = """
            {
                "code": "FILE_SIZE_MORE_THAN_ALLOWED_SIZE",
                "message": "Image resolution is more than allowed limit 10 MP, please upload after resizing image dimension or choose other image",
                "status": "error"
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        using var memStream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
        using var nonSeekable = new NonSeekableStream(memStream);

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.UploadOrgPhotoAsync(
                BiginDataCenter.US,
                SampleAccessToken,
                nonSeekable,
                "logo.png");
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
        ex.BiginErrorCode.ShouldBe(BiginErrorCodes.FileSizeMoreThanAllowedSize);
        transport.CapturedRequest.ShouldNotBeNull();
        transport.CapturedRequest.RequestUri!.AbsolutePath.ShouldBe("/bigin/v2/org/photo");
    }

    [Fact]
    public async Task GetRelatedListRecords_CompaniesModule_NormalizesToAccountsEndpoint()
    {
        var transport = new MockHttpTransport
        {
            ResponseStatusCode = HttpStatusCode.OK,
            ResponseContent = """
            {
                "data": [],
                "info": {
                    "per_page": 200,
                    "count": 0,
                    "page": 1,
                    "more_records": false
                }
            }
            """
        };

        var connector = new BiginConnector(new HttpClient(transport));
        var query = new BiginRelatedListQuery { Fields = new[] { "Deal_Name" } };

        await connector.GetRelatedListRecordsAsync(
            BiginDataCenter.US,
            SampleAccessToken,
            "Companies",
            "12345",
            "Deals",
            query);

        transport.CapturedRequest.ShouldNotBeNull();
        // Companies must be normalized to Accounts in the request URL
        transport.CapturedRequest.RequestUri!.AbsolutePath.ShouldBe("/bigin/v2/Accounts/12345/Deals");
    }

    [Fact]
    public void BiginScopes_RelatedListScopes_NormalizesCompaniesAndHandlesCasing()
    {
        BiginScopes.IsValidRelatedListReadScope("ZohoBigin.modules.ALL").ShouldBeTrue();
        BiginScopes.IsValidRelatedListReadScope("ZohoBigin.modules.Accounts.READ", "Companies").ShouldBeTrue();
        BiginScopes.IsValidRelatedListReadScope("ZohoBigin.modules.accounts.read", "Companies").ShouldBeTrue();
        BiginScopes.IsValidRelatedListReadScope("ZohoBigin.modules.contacts.read", "Contacts").ShouldBeTrue();
        BiginScopes.IsValidRelatedListReadScope("ZohoBigin.modules.Pipelines.ALL", "pipelines").ShouldBeTrue();
        BiginScopes.IsValidRelatedListReadScope("ZohoBigin.modules.other.read", "Contacts").ShouldBeFalse();
    }
}
