using Integrations.Zoho.Bigin.Webhook;
using Shouldly;
using Xunit;

namespace Pipeline.Integration.Tests.Integrations;

[Trait("Category", "Integration")]
public class ZohoBiginWebhookValidatorTests
{
    private const string SampleInsertJson = """
    {
      "server_time": 1696942911242,
      "query_params": {},
      "module": "Contacts",
      "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
      "ids": [
        "5436046000000930004"
      ],
      "affected_fields": [],
      "operation": "insert",
      "channel_id": "1001",
      "token": "TOKEN_FOR_VERIFICATION_OF_1001"
    }
    """;

    private const string SampleUpdateJson = """
    {
      "server_time": 1697093536235,
      "query_params": {},
      "module": "Contacts",
      "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
      "ids": [
        "5436046000000930004"
      ],
      "affected_fields": [
        {
          "5436046000000930004": [
            "Department",
            "Mobile",
            "Description"
          ]
        }
      ],
      "operation": "update",
      "channel_id": "1001",
      "token": "UPDATED_TOKEN_FOR_VERIFICATION_OF_1001"
    }
    """;

    [Fact]
    public void ValidateAndParse_ValidInsertPayload_ReturnsSuccess()
    {
        var result = BiginWebhookValidator.ValidateAndParse(
            SampleInsertJson,
            expectedToken: "TOKEN_FOR_VERIFICATION_OF_1001",
            expectedChannelId: "1001");

        result.IsSuccess.ShouldBeTrue();
        result.Payload.ShouldNotBeNull();
        result.Payload!.ChannelId.ShouldBe("1001");
        result.Payload.Module.ShouldBe("Contacts");
        result.Payload.Operation.ShouldBe("insert");
        result.Payload.Ids.ShouldBe(new[] { "5436046000000930004" });
        result.Payload.ServerTime.ShouldBe(1696942911242L);
        result.Payload.ServerTimeUtc.ShouldBe(DateTimeOffset.FromUnixTimeMilliseconds(1696942911242L));
        result.Payload.Token.ShouldBe("TOKEN_FOR_VERIFICATION_OF_1001");
        result.Payload.GetAffectedFieldsLookup().ShouldBeEmpty();
    }

    [Fact]
    public void ValidateAndParse_ValidUpdatePayload_ParsesAffectedFieldsLookup()
    {
        var result = BiginWebhookValidator.ValidateAndParse(
            SampleUpdateJson,
            expectedToken: "UPDATED_TOKEN_FOR_VERIFICATION_OF_1001",
            expectedChannelId: "1001");

        result.IsSuccess.ShouldBeTrue();
        result.Payload.ShouldNotBeNull();
        result.Payload!.Operation.ShouldBe("update");

        var affected = result.Payload.GetAffectedFieldsLookup();
        affected.ContainsKey("5436046000000930004").ShouldBeTrue();
        affected["5436046000000930004"].ShouldBe(new[] { "Department", "Mobile", "Description" });
    }

    [Fact]
    public void ValidateAndParse_TamperedToken_ReturnsInvalidTokenFailure()
    {
        var result = BiginWebhookValidator.ValidateAndParse(
            SampleInsertJson,
            expectedToken: "DIFFERENT_TOKEN",
            expectedChannelId: "1001");

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(BiginWebhookValidator.ErrorInvalidToken);
        result.FailureReason.ShouldNotBeNull();
        result.FailureReason.ShouldContain("does not match");
    }

    [Fact]
    public void ValidateAndParse_ChannelMismatch_ReturnsChannelMismatchFailure()
    {
        var result = BiginWebhookValidator.ValidateAndParse(
            SampleInsertJson,
            expectedToken: "TOKEN_FOR_VERIFICATION_OF_1001",
            expectedChannelId: "9999");

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(BiginWebhookValidator.ErrorChannelMismatch);
        result.FailureReason.ShouldNotBeNull();
        result.FailureReason.ShouldContain("does not match expected channel ID");
    }

    [Fact]
    public void ValidateAndParse_MissingIds_ReturnsMissingMandatory()
    {
        var invalidJson = """
        {
          "server_time": 1696942911242,
          "module": "Contacts",
          "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
          "ids": [],
          "operation": "insert",
          "channel_id": "1001"
        }
        """;

        var result = BiginWebhookValidator.ValidateAndParse(invalidJson);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(BiginWebhookValidator.ErrorMissingMandatory);
        result.FailureReason.ShouldNotBeNull();
        result.FailureReason.ShouldContain("ids");
    }

    [Fact]
    public void ValidateAndParse_UnknownModule_RejectedWhenRequired()
    {
        var unknownModuleJson = """
        {
          "server_time": 1696942911242,
          "module": "CustomUnknownModule",
          "resource_uri": "https://www.zohoapis.com/bigin/v2/CustomUnknownModule",
          "ids": ["123"],
          "operation": "insert",
          "channel_id": "1001"
        }
        """;

        var result = BiginWebhookValidator.ValidateAndParse(unknownModuleJson, requireKnownModule: true);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(BiginWebhookValidator.ErrorInvalidModule);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateAndParse_EmptyPayload_ReturnsFailure(string? empty)
    {
        var result = BiginWebhookValidator.ValidateAndParse(empty);
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(BiginWebhookValidator.ErrorEmptyPayload);
    }

    [Fact]
    public void ValidateAndParse_MalformedJson_ReturnsInvalidJsonFailure()
    {
        var result = BiginWebhookValidator.ValidateAndParse("{ not_valid_json: ");
        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe(BiginWebhookValidator.ErrorInvalidJson);
    }

    [Fact]
    public void TimingSafeEquals_EqualStrings_ReturnsTrue()
    {
        BiginWebhookValidator.TimingSafeEquals("secret_token_12345", "secret_token_12345").ShouldBeTrue();
    }

    [Fact]
    public void TimingSafeEquals_DifferentLengths_ReturnsFalse()
    {
        BiginWebhookValidator.TimingSafeEquals("short", "longer_token").ShouldBeFalse();
    }

    [Fact]
    public void TimingSafeEquals_DifferentCharacters_ReturnsFalse()
    {
        BiginWebhookValidator.TimingSafeEquals("secret_token_12345", "secret_token_12346").ShouldBeFalse();
    }

    [Fact]
    public void TimingSafeEquals_NullInputs_HandledSafely()
    {
        BiginWebhookValidator.TimingSafeEquals(null, null).ShouldBeTrue();
        BiginWebhookValidator.TimingSafeEquals("secret", null).ShouldBeFalse();
        BiginWebhookValidator.TimingSafeEquals(null, "secret").ShouldBeFalse();
    }

    [Fact]
    public void ValidateAndParse_WithNumericAndBooleanQueryParams_ParsesSuccessfully()
    {
        var jsonWithMixedQueryParams = """
        {
          "server_time": 1696942911242,
          "query_params": {
            "channel_id": "1001",
            "page": 1,
            "is_active": true
          },
          "module": "Contacts",
          "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
          "ids": [ "5436046000000930004" ],
          "operation": "insert",
          "channel_id": "1001",
          "token": "TOKEN_FOR_VERIFICATION_OF_1001"
        }
        """;

        var result = BiginWebhookValidator.ValidateAndParse(
            jsonWithMixedQueryParams,
            expectedToken: "TOKEN_FOR_VERIFICATION_OF_1001",
            expectedChannelId: "1001");

        result.IsSuccess.ShouldBeTrue();
        result.Payload.ShouldNotBeNull();
        result.Payload!.QueryParams.ShouldNotBeNull();
        result.Payload.QueryParams!["page"].ShouldBe("1");
        result.Payload.QueryParams["is_active"].ShouldBe("true");
    }

    [Fact]
    public void ValidateAndParse_CompaniesModule_Recognized()
    {
        var jsonWithCompanies = """
        {
          "server_time": 1696942911242,
          "module": "Companies",
          "resource_uri": "https://www.zohoapis.com/bigin/v2/Accounts",
          "ids": [ "5436046000000930004" ],
          "operation": "insert",
          "channel_id": "1001"
        }
        """;

        var result = BiginWebhookValidator.ValidateAndParse(jsonWithCompanies, requireKnownModule: true);
        result.IsSuccess.ShouldBeTrue();
        result.Payload.ShouldNotBeNull();
        result.Payload!.Module.ShouldBe("Companies");
    }

    [Fact]
    public void ValidateAndParse_MergedAffectedFields_CombinesDuplicateRecordIds()
    {
        var json = """
        {
          "server_time": 1697093536235,
          "module": "Contacts",
          "resource_uri": "https://www.zohoapis.com/bigin/v2/Contacts",
          "ids": [ "5436046000000930004" ],
          "affected_fields": [
            { "5436046000000930004": [ "Department" ] },
            { "5436046000000930004": [ "Mobile", "Department" ] }
          ],
          "operation": "update",
          "channel_id": "1001"
        }
        """;

        var result = BiginWebhookValidator.ValidateAndParse(json);
        result.IsSuccess.ShouldBeTrue();
        var affected = result.Payload!.GetAffectedFieldsLookup();
        affected["5436046000000930004"].ShouldBe(new[] { "Department", "Mobile" });
    }
}
