using System.Text.Json;
using Pipeline.Contracts.Requests;
using Pipeline.Contracts.Responses;
using Shouldly;
using Xunit;

namespace Pipeline.Contract.Tests.Providers.Bigin;

[Trait("Category", "Contract")]
public class BiginOrgContractTests
{
    [Fact]
    public void GetBiginOrgRequest_InitializesWithDefaultOrCustomDataCenter()
    {
        var defaultReq = new GetBiginOrgRequest();
        defaultReq.DataCenter.ShouldBeNull();

        var euReq = new GetBiginOrgRequest("EU");
        euReq.DataCenter.ShouldBe("EU");
    }

    [Fact]
    public void UploadBiginOrgPhotoRequest_RoundtripsThroughJson()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var request = new UploadBiginOrgPhotoRequest("US", bytes, "logo.png", "image/png");

        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<UploadBiginOrgPhotoRequest>(json);

        deserialized.ShouldNotBeNull();
        deserialized.DataCenter.ShouldBe("US");
        deserialized.FileName.ShouldBe("logo.png");
        deserialized.ContentType.ShouldBe("image/png");
        deserialized.FileBytes.ShouldBe(bytes);
    }

    [Fact]
    public void BiginOrgResponseContract_RoundtripsThroughJson()
    {
        var org = new BiginOrgDetailsContract(
            Id: "2034020000000013208",
            CompanyName: "Zylker Travels",
            DomainName: "org75407139",
            TimeZone: "Asia/Kolkata",
            Currency: "US Dollar",
            CurrencySymbol: "$",
            PrimaryEmail: "travels@zylker.com",
            Phone: "+1 (123) 456-7890",
            Country: "United States",
            City: "Anytown",
            McStatus: false,
            Paid: true,
            PaidType: "biginpremier",
            UsersLicensePurchased: 5);

        var response = new BiginOrgResponseContract(new List<BiginOrgDetailsContract> { org });

        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<BiginOrgResponseContract>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Org.Count.ShouldBe(1);
        var item = deserialized.Org[0];
        item.Id.ShouldBe("2034020000000013208");
        item.CompanyName.ShouldBe("Zylker Travels");
        item.Currency.ShouldBe("US Dollar");
        item.Paid.ShouldBeTrue();
        item.PaidType.ShouldBe("biginpremier");
        item.UsersLicensePurchased.ShouldBe(5);
    }

    [Fact]
    public void BiginPhotoUploadResponseContract_RoundtripsThroughJson()
    {
        var response = new BiginPhotoUploadResponseContract("SUCCESS", "photo uploaded successfully", "success");

        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<BiginPhotoUploadResponseContract>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Code.ShouldBe("SUCCESS");
        deserialized.Message.ShouldBe("photo uploaded successfully");
        deserialized.Status.ShouldBe("success");
    }

    [Fact]
    public void BiginOrgContract_NeverContainsRawSecretsOrCredentials()
    {
        var org = new BiginOrgDetailsContract(
            Id: "org_1",
            CompanyName: "Acme Corp",
            DomainName: "org123",
            TimeZone: "UTC",
            Currency: "USD",
            CurrencySymbol: "$",
            PrimaryEmail: "admin@acme.com",
            Phone: "555-1234",
            Country: "US",
            City: "Metropolis",
            McStatus: false,
            Paid: true,
            PaidType: "premier",
            UsersLicensePurchased: 10);

        var response = new BiginOrgResponseContract(new List<BiginOrgDetailsContract> { org });
        var text = response.ToString();

        text.ShouldNotContain("Bearer ");
        text.ShouldNotContain("access_token");
        text.ShouldNotContain("Zoho-oauthtoken");
        text.ShouldNotContain("secret");
    }
}
