using System.Text.Json;
using Pipeline.Contracts.Requests;
using Pipeline.Contracts.Responses;
using Shouldly;
using Xunit;

namespace Pipeline.Contract.Tests.Providers.Bigin;

[Trait("Category", "Contract")]
public class BiginRelatedRecordsContractTests
{
    [Fact]
    public void GetBiginRelatedRecordsRequest_RoundtripsThroughJson()
    {
        var request = new GetBiginRelatedRecordsRequest(
            ModuleApiName: "Contacts",
            RecordId: "2034020000000478028",
            RelatedListApiName: "Deals",
            Fields: new[] { "Deal_Name", "Stage" },
            Page: 2,
            PerPage: 50,
            DataCenter: "EU");

        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<GetBiginRelatedRecordsRequest>(json);

        deserialized.ShouldNotBeNull();
        deserialized.ModuleApiName.ShouldBe("Contacts");
        deserialized.RecordId.ShouldBe("2034020000000478028");
        deserialized.RelatedListApiName.ShouldBe("Deals");
        deserialized.Fields.ShouldBe(new[] { "Deal_Name", "Stage" });
        deserialized.Page.ShouldBe(2);
        deserialized.PerPage.ShouldBe(50);
        deserialized.DataCenter.ShouldBe("EU");
    }

    [Fact]
    public void BiginRelatedRecordsResponseContract_RoundtripsThroughJson()
    {
        const string rawItems = """
        [
            { "id": "deal_1", "Deal_Name": "Safari Tour" },
            { "id": "deal_2", "Deal_Name": "City Pass" }
        ]
        """;

        using var doc = JsonDocument.Parse(rawItems);
        var records = doc.RootElement.EnumerateArray().Select(el => el.Clone()).ToList();

        var response = new BiginRelatedRecordsResponseContract(
            records,
            Page: 1,
            PerPage: 200,
            Count: 2,
            MoreRecords: false);

        var json = JsonSerializer.Serialize(response);
        var deserialized = JsonSerializer.Deserialize<BiginRelatedRecordsResponseContract>(json);

        deserialized.ShouldNotBeNull();
        deserialized.Records.Count.ShouldBe(2);
        deserialized.Page.ShouldBe(1);
        deserialized.PerPage.ShouldBe(200);
        deserialized.Count.ShouldBe(2);
        deserialized.MoreRecords.ShouldBeFalse();
        deserialized.Records[0].GetProperty("Deal_Name").GetString().ShouldBe("Safari Tour");
    }
}
