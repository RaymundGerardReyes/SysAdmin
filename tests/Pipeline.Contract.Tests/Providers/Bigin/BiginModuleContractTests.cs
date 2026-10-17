using System.Text.Json;
using Pipeline.Contracts.Requests;
using Pipeline.Contracts.Responses;
using Shouldly;
using Xunit;

namespace Pipeline.Contract.Tests.Providers.Bigin;

[Trait("Category", "Contract")]
public class BiginModuleContractTests
{
    [Fact]
    public void BiginModuleItemResponse_RoundtripsThroughJson()
    {
        var item = new BiginModuleItemResponse(
            "2034020000000000127",
            "Accounts",
            "Accounts",
            "Company",
            "Companies",
            true,
            true,
            true,
            true,
            true,
            true,
            1,
            "default",
            4,
            true);

        var response = new BiginModulesResponseContract(new List<BiginModuleItemResponse> { item });

        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<BiginModulesResponseContract>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Modules.Count.ShouldBe(1);
        var mod = deserialized.Modules[0];
        mod.Id.ShouldBe("2034020000000000127");
        mod.ApiName.ShouldBe("Accounts");
        mod.SingularLabel.ShouldBe("Company");
        mod.PluralLabel.ShouldBe("Companies");
        mod.ApiSupported.ShouldBeTrue();
        mod.GeneratedType.ShouldBe("default");
        mod.SequenceNumber.ShouldBe(4);
    }

    [Fact]
    public void GetBiginModulesRequest_InitializesWithDefaultOrCustomDataCenter()
    {
        var defaultReq = new GetBiginModulesRequest();
        defaultReq.DataCenter.ShouldBeNull();

        var euReq = new GetBiginModulesRequest("EU");
        euReq.DataCenter.ShouldBe("EU");
    }

    [Fact]
    public void BiginContract_NeverContainsRawSecretsOrCredentials()
    {
        var item = new BiginModuleItemResponse(
            "mod_1",
            "Accounts",
            "Accounts",
            "Company",
            "Companies",
            true,
            true,
            true,
            true,
            true,
            true,
            1,
            "default",
            1,
            true);

        var response = new BiginModulesResponseContract(new List<BiginModuleItemResponse> { item });
        var text = response.ToString();

        text.ShouldNotContain("Bearer ");
        text.ShouldNotContain("access_token");
        text.ShouldNotContain("Zoho-oauthtoken");
        text.ShouldNotContain("secret");
    }

    [Fact]
    public void BiginModulesResponseContract_SerializesEmptyList()
    {
        var response = new BiginModulesResponseContract(Array.Empty<BiginModuleItemResponse>());
        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<BiginModulesResponseContract>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Modules.ShouldNotBeNull();
        deserialized.Modules.Count.ShouldBe(0);
    }
}
