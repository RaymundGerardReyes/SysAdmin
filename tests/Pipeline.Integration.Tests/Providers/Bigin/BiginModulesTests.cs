using System.Net;
using System.Text.Json;
using Integrations.Zoho.Bigin;
using Integrations.Zoho.Bigin.Models;
using Shouldly;
using Xunit;

namespace Pipeline.Integration.Tests.Providers.Bigin;

[Trait("Category", "Integration")]
public class BiginModulesTests
{
    private const string SampleModulesJsonResponse = """
    {
        "modules": [
            {
                "has_more_profiles": false,
                "global_search_supported": true,
                "deletable": true,
                "description": null,
                "creatable": true,
                "email_parser_supported": true,
                "inventory_template_supported": false,
                "modified_time": null,
                "plural_label": "Contacts",
                "presence_sub_menu": true,
                "triggers_supported": true,
                "id": "2034020000000000129",
                "isBlueprintSupported": true,
                "visible": true,
                "visibility": 1,
                "convertable": false,
                "editable": true,
                "emailTemplate_support": true,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": true,
                "show_as_tab": true,
                "web_link": null,
                "sequence_number": 3,
                "singular_label": "Contact",
                "viewable": true,
                "api_supported": true,
                "api_name": "Contacts",
                "quick_create": true,
                "modified_by": null,
                "generated_type": "default",
                "feeds_required": true,
                "scoring_supported": true,
                "webform_supported": true,
                "arguments": [],
                "module_name": "Contacts",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {}
            },
            {
                "has_more_profiles": false,
                "global_search_supported": true,
                "deletable": true,
                "description": null,
                "creatable": true,
                "email_parser_supported": true,
                "inventory_template_supported": false,
                "modified_time": "2023-04-11T17:00:27+05:30",
                "plural_label": "Companies",
                "presence_sub_menu": true,
                "triggers_supported": true,
                "id": "2034020000000000127",
                "isBlueprintSupported": true,
                "visible": true,
                "visibility": 1,
                "convertable": false,
                "editable": true,
                "emailTemplate_support": true,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": true,
                "show_as_tab": true,
                "web_link": null,
                "sequence_number": 4,
                "singular_label": "Company",
                "viewable": true,
                "api_supported": true,
                "api_name": "Accounts",
                "quick_create": true,
                "modified_by": {
                    "name": "Zylker Travels",
                    "id": "2034020000000457001"
                },
                "generated_type": "default",
                "feeds_required": true,
                "scoring_supported": true,
                "webform_supported": false,
                "arguments": [],
                "module_name": "Accounts",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {}
            },
            {
                "has_more_profiles": false,
                "global_search_supported": true,
                "deletable": true,
                "description": null,
                "creatable": true,
                "email_parser_supported": false,
                "inventory_template_supported": false,
                "modified_time": "2023-05-10T19:28:46+05:30",
                "plural_label": "Pipelines",
                "presence_sub_menu": true,
                "triggers_supported": true,
                "id": "2034020000000000131",
                "isBlueprintSupported": true,
                "visible": true,
                "visibility": 1,
                "convertable": false,
                "editable": true,
                "emailTemplate_support": true,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": true,
                "show_as_tab": true,
                "web_link": null,
                "sequence_number": 5,
                "singular_label": "Pipeline",
                "viewable": true,
                "api_supported": true,
                "api_name": "Pipelines",
                "quick_create": true,
                "modified_by": {
                    "name": "Zylker Travels",
                    "id": "2034020000000457001"
                },
                "generated_type": "default",
                "feeds_required": true,
                "scoring_supported": true,
                "webform_supported": true,
                "arguments": [],
                "module_name": "Pipelines",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {}
            },
            {
                "has_more_profiles": false,
                "global_search_supported": true,
                "deletable": true,
                "description": null,
                "creatable": true,
                "email_parser_supported": false,
                "inventory_template_supported": false,
                "modified_time": null,
                "plural_label": "Tasks",
                "presence_sub_menu": true,
                "triggers_supported": true,
                "id": "2034020000000000143",
                "isBlueprintSupported": true,
                "visible": true,
                "visibility": 1,
                "convertable": false,
                "editable": true,
                "emailTemplate_support": true,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": true,
                "show_as_tab": true,
                "web_link": null,
                "sequence_number": 6,
                "singular_label": "Task",
                "viewable": true,
                "api_supported": true,
                "api_name": "Tasks",
                "quick_create": true,
                "modified_by": null,
                "generated_type": "default",
                "feeds_required": true,
                "scoring_supported": false,
                "webform_supported": false,
                "arguments": [],
                "module_name": "Tasks",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {}
            },
            {
                "has_more_profiles": false,
                "global_search_supported": true,
                "deletable": true,
                "description": null,
                "creatable": true,
                "email_parser_supported": false,
                "inventory_template_supported": false,
                "modified_time": null,
                "plural_label": "Events",
                "presence_sub_menu": true,
                "triggers_supported": true,
                "id": "2034020000000000145",
                "isBlueprintSupported": true,
                "visible": true,
                "visibility": 1,
                "convertable": false,
                "editable": true,
                "emailTemplate_support": true,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": true,
                "show_as_tab": true,
                "web_link": null,
                "sequence_number": 6,
                "singular_label": "Event",
                "viewable": true,
                "api_supported": true,
                "api_name": "Events",
                "quick_create": true,
                "modified_by": null,
                "generated_type": "default",
                "feeds_required": true,
                "scoring_supported": false,
                "webform_supported": false,
                "arguments": [],
                "module_name": "Events",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {}
            },
            {
                "has_more_profiles": false,
                "global_search_supported": true,
                "deletable": true,
                "description": null,
                "creatable": true,
                "email_parser_supported": false,
                "inventory_template_supported": false,
                "modified_time": null,
                "plural_label": "Calls",
                "presence_sub_menu": true,
                "triggers_supported": true,
                "id": "2034020000000017015",
                "isBlueprintSupported": false,
                "visible": true,
                "visibility": 1,
                "convertable": false,
                "editable": true,
                "emailTemplate_support": true,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": true,
                "show_as_tab": true,
                "web_link": null,
                "sequence_number": 6,
                "singular_label": "Call",
                "viewable": true,
                "api_supported": true,
                "api_name": "Calls",
                "quick_create": true,
                "modified_by": null,
                "generated_type": "default",
                "feeds_required": true,
                "scoring_supported": false,
                "webform_supported": false,
                "arguments": [],
                "module_name": "Calls",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {}
            },
            {
                "has_more_profiles": false,
                "global_search_supported": true,
                "deletable": true,
                "description": null,
                "creatable": true,
                "email_parser_supported": false,
                "inventory_template_supported": false,
                "modified_time": null,
                "plural_label": "Products",
                "presence_sub_menu": true,
                "triggers_supported": true,
                "id": "2034020000000000163",
                "isBlueprintSupported": true,
                "visible": true,
                "visibility": 1,
                "convertable": false,
                "editable": true,
                "emailTemplate_support": true,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": true,
                "show_as_tab": true,
                "web_link": null,
                "sequence_number": 9,
                "singular_label": "Product",
                "viewable": true,
                "api_supported": true,
                "api_name": "Products",
                "quick_create": true,
                "modified_by": null,
                "generated_type": "default",
                "feeds_required": true,
                "scoring_supported": false,
                "webform_supported": false,
                "arguments": [],
                "module_name": "Products",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {}
            },
            {
                "has_more_profiles": false,
                "global_search_supported": false,
                "deletable": false,
                "description": null,
                "creatable": false,
                "email_parser_supported": false,
                "inventory_template_supported": false,
                "modified_time": null,
                "plural_label": "Social",
                "presence_sub_menu": false,
                "triggers_supported": false,
                "id": "2034020000000048001",
                "isBlueprintSupported": false,
                "visible": true,
                "visibility": 1,
                "convertable": false,
                "editable": false,
                "emailTemplate_support": false,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    }
                ],
                "filter_supported": false,
                "show_as_tab": true,
                "web_link": null,
                "sequence_number": 24,
                "singular_label": "Social",
                "viewable": true,
                "api_supported": false,
                "api_name": "Social",
                "quick_create": false,
                "modified_by": null,
                "generated_type": "default",
                "feeds_required": false,
                "scoring_supported": false,
                "webform_supported": false,
                "arguments": [],
                "module_name": "Social",
                "profile_count": 1,
                "business_card_field_limit": 5,
                "parent_module": {}
            },
            {
                "has_more_profiles": false,
                "global_search_supported": true,
                "deletable": true,
                "description": null,
                "creatable": false,
                "email_parser_supported": false,
                "inventory_template_supported": false,
                "modified_time": "2023-04-11T17:00:34+05:30",
                "plural_label": "Associated Products",
                "presence_sub_menu": false,
                "triggers_supported": false,
                "id": "2034020000000458818",
                "isBlueprintSupported": false,
                "visible": true,
                "visibility": 2,
                "convertable": false,
                "editable": false,
                "emailTemplate_support": false,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": false,
                "show_as_tab": false,
                "web_link": null,
                "sequence_number": 25,
                "singular_label": "Associated Products",
                "viewable": true,
                "api_supported": true,
                "api_name": "Associated_Products",
                "quick_create": false,
                "modified_by": {
                    "name": "Zylker Travels",
                    "id": "2034020000000457001"
                },
                "generated_type": "subform",
                "feeds_required": false,
                "scoring_supported": false,
                "webform_supported": false,
                "arguments": [],
                "module_name": "LinkingModule1",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {
                    "api_name": "Deals",
                    "id": "2034020000000000131"
                }
            },
            {
                "has_more_profiles": false,
                "global_search_supported": true,
                "deletable": true,
                "description": null,
                "creatable": true,
                "email_parser_supported": false,
                "inventory_template_supported": false,
                "modified_time": null,
                "plural_label": "Notes",
                "presence_sub_menu": false,
                "triggers_supported": false,
                "id": "2034020000000000147",
                "isBlueprintSupported": false,
                "visible": false,
                "visibility": 1,
                "convertable": false,
                "editable": true,
                "emailTemplate_support": false,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": false,
                "show_as_tab": false,
                "web_link": null,
                "sequence_number": 29,
                "singular_label": "Note",
                "viewable": true,
                "api_supported": true,
                "api_name": "Notes",
                "quick_create": false,
                "modified_by": null,
                "generated_type": "default",
                "feeds_required": false,
                "scoring_supported": false,
                "webform_supported": false,
                "arguments": [],
                "module_name": "Notes",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {}
            },
            {
                "has_more_profiles": false,
                "global_search_supported": false,
                "deletable": true,
                "description": null,
                "creatable": true,
                "email_parser_supported": false,
                "inventory_template_supported": false,
                "modified_time": null,
                "plural_label": "Attachments",
                "presence_sub_menu": false,
                "triggers_supported": false,
                "id": "2034020000000000177",
                "isBlueprintSupported": false,
                "visible": false,
                "visibility": 1,
                "convertable": false,
                "editable": false,
                "emailTemplate_support": false,
                "profiles": [
                    {
                        "name": "Administrator",
                        "id": "2034020000000015972"
                    },
                    {
                        "name": "Standard",
                        "id": "2034020000000015975"
                    }
                ],
                "filter_supported": false,
                "show_as_tab": false,
                "web_link": null,
                "sequence_number": 33,
                "singular_label": "Attachment",
                "viewable": true,
                "api_supported": true,
                "api_name": "Attachments",
                "quick_create": false,
                "modified_by": null,
                "generated_type": "default",
                "feeds_required": false,
                "scoring_supported": false,
                "webform_supported": false,
                "arguments": [],
                "module_name": "Attachments",
                "profile_count": 2,
                "business_card_field_limit": 5,
                "parent_module": {}
            }
        ]
    }
    """;

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }

    [Fact]
    public async Task GetModulesAsync_Success_ParsesAllFieldsAndModules()
    {
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            req.Method.ShouldBe(HttpMethod.Get);
            req.RequestUri!.ToString().ShouldBe("https://www.zohoapis.com/bigin/v2/settings/modules");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SampleModulesJsonResponse)
            };
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var result = await connector.GetModulesAsync(BiginDataCenter.US, "sample_oauth_token");

        result.ShouldNotBeNull();
        result.Modules.Count.ShouldBe(11);

        // Verify Contacts module
        var contact = result.Modules.First(m => m.ApiName == "Contacts");
        contact.Id.ShouldBe("2034020000000000129");
        contact.ModuleName.ShouldBe("Contacts");
        contact.SingularLabel.ShouldBe("Contact");
        contact.PluralLabel.ShouldBe("Contacts");
        contact.IsBlueprintSupported.ShouldBeTrue();
        contact.EmailTemplateSupport.ShouldBeTrue();
        contact.Creatable.ShouldBeTrue();
        contact.Editable.ShouldBeTrue();
        contact.Deletable.ShouldBeTrue();
        contact.Viewable.ShouldBeTrue();
        contact.Visible.ShouldBeTrue();
        contact.Visibility.ShouldBe(1);
        contact.QuickCreate.ShouldBeTrue();
        contact.GeneratedType.ShouldBe(BiginGeneratedTypes.Default);
        contact.Profiles.ShouldNotBeNull();
        contact.Profiles.Count.ShouldBe(2);
        contact.Profiles[0].Name.ShouldBe("Administrator");
        contact.Profiles[1].Name.ShouldBe("Standard");
        contact.BusinessCardFieldLimit.ShouldBe(5);
        contact.ParentModule.ShouldNotBeNull();
        contact.ParentModule.ApiName.ShouldBeNull();

        // Verify Accounts module (Companies)
        var account = result.Modules.First(m => m.ApiName == "Accounts");
        account.Id.ShouldBe("2034020000000000127");
        account.ModuleName.ShouldBe("Accounts");
        account.SingularLabel.ShouldBe("Company");
        account.PluralLabel.ShouldBe("Companies");
        account.ModifiedTime.ShouldNotBeNull();
        account.ModifiedTime.Value.Offset.ShouldBe(TimeSpan.FromHours(5.5)); // +05:30
        account.ModifiedBy.ShouldNotBeNull();
        account.ModifiedBy.Name.ShouldBe("Zylker Travels");
        account.ModifiedBy.Id.ShouldBe("2034020000000457001");

        // Verify Associated Products (subform linking module)
        var assoc = result.Modules.First(m => m.ApiName == "Associated_Products");
        assoc.Id.ShouldBe("2034020000000458818");
        assoc.GeneratedType.ShouldBe(BiginGeneratedTypes.Subform);
        assoc.Visibility.ShouldBe(2);
        assoc.ParentModule.ShouldNotBeNull();
        assoc.ParentModule.ApiName.ShouldBe("Deals");
        assoc.ParentModule.Id.ShouldBe("2034020000000000131");

        // Verify Social (non API supported)
        var social = result.Modules.First(m => m.ApiName == "Social");
        social.ApiSupported.ShouldBeFalse();
        social.QuickCreate.ShouldBeFalse();
        social.ProfileCount.ShouldBe(1);

        // Verify Notes and Attachments
        var notes = result.Modules.First(m => m.ApiName == "Notes");
        notes.Visible.ShouldBeFalse();
        notes.SequenceNumber.ShouldBe(29);

        var attachments = result.Modules.First(m => m.ApiName == "Attachments");
        attachments.Visible.ShouldBeFalse();
        attachments.SequenceNumber.ShouldBe(33);
    }

    [Theory]
    [InlineData(BiginDataCenter.US, "https://www.zohoapis.com/bigin/v2/settings/modules")]
    [InlineData(BiginDataCenter.EU, "https://www.zohoapis.eu/bigin/v2/settings/modules")]
    [InlineData(BiginDataCenter.AU, "https://www.zohoapis.com.au/bigin/v2/settings/modules")]
    [InlineData(BiginDataCenter.IN, "https://www.zohoapis.in/bigin/v2/settings/modules")]
    [InlineData(BiginDataCenter.CN, "https://www.zohoapis.com.cn/bigin/v2/settings/modules")]
    [InlineData(BiginDataCenter.JP, "https://www.zohoapis.jp/bigin/v2/settings/modules")]
    [InlineData(BiginDataCenter.SA, "https://www.zohoapis.sa/bigin/v2/settings/modules")]
    [InlineData(BiginDataCenter.CA, "https://www.zohoapis.ca/bigin/v2/settings/modules")]
    public async Task GetModulesAsync_AllDataCenters_RouteToCorrectDomains(BiginDataCenter dc, string expectedUrl)
    {
        string? capturedUrl = null;
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            capturedUrl = req.RequestUri!.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SampleModulesJsonResponse)
            };
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var result = await connector.GetModulesAsync(dc, "test_token");

        result.ShouldNotBeNull();
        capturedUrl.ShouldBe(expectedUrl);
    }

    [Fact]
    public async Task GetModulesAsync_AuthorizationHeader_IncludesZohoOAuthToken()
    {
        HttpRequestMessage? captured = null;
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SampleModulesJsonResponse)
            };
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        await connector.GetModulesAsync(BiginDataCenter.US, "1000.sample_token_xyz");

        captured.ShouldNotBeNull();
        captured.Headers.Contains("Authorization").ShouldBeTrue();
        captured.Headers.GetValues("Authorization").First().ShouldBe("Zoho-oauthtoken 1000.sample_token_xyz");
    }

    [Fact]
    public async Task GetModulesAsync_Status404_InvalidUrlPattern_ThrowsBiginApiException()
    {
        var errorJson = """{"code": "INVALID_URL_PATTERN", "message": "Please check if the URL trying to access is a correct one"}""";
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(errorJson)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.GetModulesAsync(BiginDataCenter.US);
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ex.BiginErrorCode.ShouldBe(BiginErrorCodes.InvalidUrlPattern);
        ex.BiginMessage.ShouldContain("Please check if the URL trying to access is a correct one");
    }

    [Fact]
    public async Task GetModulesAsync_Status401_OAuthScopeMismatch_ThrowsBiginApiException()
    {
        var errorJson = """{"code": "OAUTH_SCOPE_MISMATCH", "message": "invalid oauth scope to access this URL"}""";
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent(errorJson)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.GetModulesAsync(BiginDataCenter.US);
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ex.BiginErrorCode.ShouldBe(BiginErrorCodes.OAuthScopeMismatch);
        ex.BiginMessage.ShouldContain("invalid oauth scope");
    }

    [Fact]
    public async Task GetModulesAsync_Status403_NoPermission_ThrowsBiginApiException()
    {
        var errorJson = """{"code": "NO_PERMISSION", "message": "Permission denied to read"}""";
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(errorJson)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.GetModulesAsync(BiginDataCenter.US);
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ex.BiginErrorCode.ShouldBe(BiginErrorCodes.NoPermission);
        ex.BiginMessage.ShouldContain("Permission denied to read");
    }

    [Fact]
    public async Task GetModulesAsync_Status400_AuthorizationFailed_ThrowsBiginApiException()
    {
        var errorJson = """{"code": "AUTHORIZATION_FAILED", "message": "User does not have sufficient privilege to read module details"}""";
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorJson)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.GetModulesAsync(BiginDataCenter.US);
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ex.BiginErrorCode.ShouldBe(BiginErrorCodes.AuthorizationFailed);
        ex.BiginMessage.ShouldContain("User does not have sufficient privilege");
    }

    [Fact]
    public async Task GetModulesAsync_Status400_InvalidRequestMethod_ThrowsBiginApiException()
    {
        var errorJson = """{"code": "INVALID_REQUEST_METHOD", "message": "The http request method type is not a valid one"}""";
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorJson)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var ex = await Should.ThrowAsync<BiginApiException>(async () =>
        {
            await connector.GetModulesAsync(BiginDataCenter.US);
        });

        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ex.BiginErrorCode.ShouldBe(BiginErrorCodes.InvalidRequestMethod);
    }

    [Fact]
    public async Task GetModulesAsync_MalformedJson_ThrowsJsonException()
    {
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ modules: broken json")
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        await Should.ThrowAsync<JsonException>(async () =>
        {
            await connector.GetModulesAsync(BiginDataCenter.US);
        });
    }

    [Fact]
    public async Task GetModuleByApiNameAsync_FindsModuleCaseInsensitive()
    {
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(SampleModulesJsonResponse)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var accountModule = await connector.GetModuleByApiNameAsync("accounts");
        accountModule.ShouldNotBeNull();
        accountModule.ApiName.ShouldBe("Accounts");
        accountModule.SingularLabel.ShouldBe("Company");

        var nonExistent = await connector.GetModuleByApiNameAsync("NonExistent_Module");
        nonExistent.ShouldBeNull();
    }

    [Fact]
    public async Task GetModuleByIdAsync_FindsModule()
    {
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(SampleModulesJsonResponse)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var module = await connector.GetModuleByIdAsync("2034020000000000131");
        module.ShouldNotBeNull();
        module.ApiName.ShouldBe("Pipelines");
    }

    [Fact]
    public async Task GetDefaultAndCreatableModules_FiltersCorrectly()
    {
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(SampleModulesJsonResponse)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var defaultModules = await connector.GetDefaultModulesAsync();
        defaultModules.Count.ShouldBe(10); // 10 out of 11 are "default", 1 is "subform"

        var creatable = await connector.GetCreatableModulesAsync();
        creatable.All(m => m.Creatable && m.ApiSupported).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Contacts", true)]
    [InlineData("Accounts", true)]
    [InlineData("Pipelines", true)]
    [InlineData("Associated_Products", true)]
    [InlineData("Custom_Module_123", true)]
    [InlineData("module", true)]
    [InlineData("a_b_c", true)]
    [InlineData("1Contacts", false)]       // Starts with digit
    [InlineData("_Contacts", false)]       // Starts with underscore
    [InlineData("Contacts_", false)]       // Ends with underscore
    [InlineData("Contacts__Two", false)]   // Consecutive underscores
    [InlineData("Contacts Module", false)] // Contains space
    [InlineData("Contacts-Module", false)] // Contains hyphen
    [InlineData("", false)]                // Empty
    [InlineData(null, false)]              // Null
    public void ApiNameValidator_ValidatesPerZohoSpec(string? apiName, bool expectedValid)
    {
        BiginApiNameValidator.IsValid(apiName).ShouldBe(expectedValid);
        if (expectedValid)
        {
            Should.NotThrow(() => BiginApiNameValidator.ValidateOrThrow(apiName));
        }
        else
        {
            Should.Throw<ArgumentException>(() => BiginApiNameValidator.ValidateOrThrow(apiName));
        }
    }

    [Theory]
    [InlineData("US", BiginDataCenter.US)]
    [InlineData("eu", BiginDataCenter.EU)]
    [InlineData("AU", BiginDataCenter.AU)]
    [InlineData("in", BiginDataCenter.IN)]
    [InlineData("cn", BiginDataCenter.CN)]
    [InlineData("jp", BiginDataCenter.JP)]
    [InlineData("sa", BiginDataCenter.SA)]
    [InlineData("ca", BiginDataCenter.CA)]
    public void BiginDataCenterExtensions_TryParse_Succeeds(string input, BiginDataCenter expected)
    {
        var success = BiginDataCenterExtensions.TryParse(input, out var dc);
        success.ShouldBeTrue();
        dc.ShouldBe(expected);
    }

    [Fact]
    public void BiginScopes_ContainsDocumentedScopes()
    {
        BiginScopes.IsValidModulesReadScope(BiginScopes.SettingsAll).ShouldBeTrue();
        BiginScopes.IsValidModulesReadScope(BiginScopes.SettingsModulesAll).ShouldBeTrue();
        BiginScopes.IsValidModulesReadScope(BiginScopes.SettingsModulesRead).ShouldBeTrue();
        BiginScopes.IsValidModulesReadScope("ZohoBigin.unauthorized.scope").ShouldBeFalse();
    }

    [Fact]
    public async Task GetModuleByApiNameAsync_WithCompaniesOrCompany_ResolvesAccounts()
    {
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(SampleModulesJsonResponse)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var byPlural = await connector.GetModuleByApiNameAsync("Companies");
        byPlural.ShouldNotBeNull();
        byPlural.ApiName.ShouldBe("Accounts");
        byPlural.SingularLabel.ShouldBe("Company");

        var bySingular = await connector.GetModuleByApiNameAsync("Company");
        bySingular.ShouldNotBeNull();
        bySingular.ApiName.ShouldBe("Accounts");
    }

    [Fact]
    public async Task GetModuleByApiNameAsync_WithAssociatedProducts_ResolvesAssociatedProducts()
    {
        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(SampleModulesJsonResponse)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var module = await connector.GetModuleByApiNameAsync("Associated Products");
        module.ShouldNotBeNull();
        module.ApiName.ShouldBe("Associated_Products");
        module.GeneratedType.ShouldBe(BiginGeneratedTypes.Subform);
    }

    [Fact]
    public async Task GetModulesAsync_ResilientDateTimeOffsetHandling()
    {
        var jsonWithVariousDateFormats = """
        {
            "modules": [
                {
                    "id": "1",
                    "module_name": "Mod1",
                    "api_name": "Mod1",
                    "singular_label": "Mod1",
                    "plural_label": "Mod1s",
                    "modified_time": "",
                    "visible": true,
                    "visibility": 1,
                    "viewable": true,
                    "creatable": true,
                    "editable": true,
                    "deletable": true,
                    "convertable": false,
                    "quick_create": false,
                    "api_supported": true,
                    "global_search_supported": true,
                    "email_parser_supported": false,
                    "inventory_template_supported": false,
                    "isBlueprintSupported": false,
                    "emailTemplate_support": false,
                    "filter_supported": false,
                    "show_as_tab": true,
                    "sequence_number": 1,
                    "generated_type": "default",
                    "feeds_required": false,
                    "scoring_supported": false,
                    "webform_supported": false,
                    "profile_count": 1,
                    "business_card_field_limit": 5,
                    "presence_sub_menu": false,
                    "triggers_supported": false,
                    "has_more_profiles": false
                },
                {
                    "id": "2",
                    "module_name": "Mod2",
                    "api_name": "Mod2",
                    "singular_label": "Mod2",
                    "plural_label": "Mod2s",
                    "modified_time": "   ",
                    "visible": true,
                    "visibility": 1,
                    "viewable": true,
                    "creatable": true,
                    "editable": true,
                    "deletable": true,
                    "convertable": false,
                    "quick_create": false,
                    "api_supported": true,
                    "global_search_supported": true,
                    "email_parser_supported": false,
                    "inventory_template_supported": false,
                    "isBlueprintSupported": false,
                    "emailTemplate_support": false,
                    "filter_supported": false,
                    "show_as_tab": true,
                    "sequence_number": 2,
                    "generated_type": "default",
                    "feeds_required": false,
                    "scoring_supported": false,
                    "webform_supported": false,
                    "profile_count": 1,
                    "business_card_field_limit": 5,
                    "presence_sub_menu": false,
                    "triggers_supported": false,
                    "has_more_profiles": false
                },
                {
                    "id": "3",
                    "module_name": "Mod3",
                    "api_name": "Mod3",
                    "singular_label": "Mod3",
                    "plural_label": "Mod3s",
                    "modified_time": "2023-04-11T11:30:27Z",
                    "visible": true,
                    "visibility": 1,
                    "viewable": true,
                    "creatable": true,
                    "editable": true,
                    "deletable": true,
                    "convertable": false,
                    "quick_create": false,
                    "api_supported": true,
                    "global_search_supported": true,
                    "email_parser_supported": false,
                    "inventory_template_supported": false,
                    "isBlueprintSupported": false,
                    "emailTemplate_support": false,
                    "filter_supported": false,
                    "show_as_tab": true,
                    "sequence_number": 3,
                    "generated_type": "default",
                    "feeds_required": false,
                    "scoring_supported": false,
                    "webform_supported": false,
                    "profile_count": 1,
                    "business_card_field_limit": 5,
                    "presence_sub_menu": false,
                    "triggers_supported": false,
                    "has_more_profiles": false
                }
            ]
        }
        """;

        var mockHandler = new MockHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonWithVariousDateFormats)
        });

        using var client = new HttpClient(mockHandler);
        var connector = new BiginConnector(client);

        var result = await connector.GetModulesAsync();
        result.Modules.Count.ShouldBe(3);
        result.Modules[0].ModifiedTime.ShouldBeNull();
        result.Modules[1].ModifiedTime.ShouldBeNull();
        result.Modules[2].ModifiedTime.ShouldNotBeNull();
        result.Modules[2].ModifiedTime!.Value.UtcDateTime.Year.ShouldBe(2023);
    }

    [Fact]
    public void BiginApiException_FromHttpResponse_ParsesArrayErrorStructure()
    {
        var arrayErrorJson = """
        [
            {
                "code": "OAUTH_SCOPE_MISMATCH",
                "message": "invalid oauth scope to access this URL",
                "status": "error"
            }
        ]
        """;

        var ex = BiginApiException.FromHttpResponse(HttpStatusCode.Unauthorized, arrayErrorJson, "https://www.zohoapis.com/bigin/v2/settings/modules");

        ex.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        ex.BiginErrorCode.ShouldBe(BiginErrorCodes.OAuthScopeMismatch);
        ex.BiginMessage.ShouldBe("invalid oauth scope to access this URL");
        ex.BiginStatus.ShouldBe("error");
        ex.RawResponseBody.ShouldBe(arrayErrorJson);
        ex.RequestUrl.ShouldBe("https://www.zohoapis.com/bigin/v2/settings/modules");
    }

    [Fact]
    public void BiginApiException_FromHttpResponse_HandlesNonJsonGracefully()
    {
        var rawHtml = "<html><body>502 Bad Gateway</body></html>";
        var ex = BiginApiException.FromHttpResponse(HttpStatusCode.BadGateway, rawHtml, "https://www.zohoapis.com/bigin/v2/settings/modules");

        ex.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        ex.BiginErrorCode.ShouldBeNull();
        ex.BiginMessage.ShouldContain("502");
        ex.RawResponseBody.ShouldBe(rawHtml);
    }

    [Theory]
    [InlineData("Companies", "Accounts")]
    [InlineData("Company", "Accounts")]
    [InlineData("companies", "Accounts")]
    [InlineData("Associated Products", "Associated_Products")]
    [InlineData("AssociatedProducts", "Associated_Products")]
    [InlineData("LinkingModule1", "Associated_Products")]
    [InlineData("Pipelines", "Pipelines")]
    [InlineData("Contacts", "Contacts")]
    [InlineData("Accounts", "Accounts")]
    public void NormalizeApiName_MapsKnownDisplayNamesToApiNames(string input, string expected)
    {
        var normalized = BiginApiNameValidator.NormalizeApiName(input);
        normalized.ShouldBe(expected);
    }

    [Fact]
    public void BiginConnector_Implements_IBiginConnector()
    {
        typeof(IBiginConnector).IsAssignableFrom(typeof(BiginConnector)).ShouldBeTrue();
    }

    [Fact]
    public void BiginModules_ContainsAllDocumentedStandardModules()
    {
        BiginModules.All.Contains("Contacts").ShouldBeTrue();
        BiginModules.All.Contains("Pipelines").ShouldBeTrue();
        BiginModules.All.Contains("Accounts").ShouldBeTrue();
        BiginModules.All.Contains("Products").ShouldBeTrue();
        BiginModules.All.Contains("Tasks").ShouldBeTrue();
        BiginModules.All.Contains("Events").ShouldBeTrue();
        BiginModules.All.Contains("Calls").ShouldBeTrue();
        BiginModules.All.Contains("Associated_Products").ShouldBeTrue();
        BiginModules.All.Contains("Notes").ShouldBeTrue();
        BiginModules.All.Contains("Attachments").ShouldBeTrue();
        BiginModules.All.Contains("Social").ShouldBeTrue();
    }
}
