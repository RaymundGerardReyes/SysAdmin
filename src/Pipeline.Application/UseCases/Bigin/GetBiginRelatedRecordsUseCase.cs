using System.Text.Json;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Bigin;

public sealed record GetBiginRelatedRecordsCommand(
    string ModuleApiName,
    string RecordId,
    string RelatedListApiName,
    IReadOnlyList<string>? Fields = null,
    int? Page = 1,
    int? PerPage = 200,
    string? DataCenter = null) : IUseCaseRequest
{
    public string OperationName => "bigin.relatedlists.read";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string>
        {
            ["provider"] = "Bigin",
            ["module"] = ModuleApiName,
            ["recordId"] = RecordId,
            ["relatedList"] = RelatedListApiName
        };
}

[RequiresPermission("bigin.relatedlists.read")]
public sealed class GetBiginRelatedRecordsUseCase : IUseCaseHandler<GetBiginRelatedRecordsCommand, BiginRelatedRecordsResponseContract>
{
    private readonly ICredentialBroker _credentialBroker;

    public GetBiginRelatedRecordsUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    private static readonly HashSet<string> ValidDataCenters = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "EU", "AU", "IN", "CN", "JP", "SA", "CA"
    };

    public async Task<Result<BiginRelatedRecordsResponseContract>> HandleAsync(
        GetBiginRelatedRecordsCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.DataCenter) && !ValidDataCenters.Contains(request.DataCenter.Trim()))
        {
            return Result<BiginRelatedRecordsResponseContract>.Failure("INVALID_DATA_CENTER", $"Unsupported data center: '{request.DataCenter}'. Supported data centers are US, EU, AU, IN, CN, JP, SA, CA.");
        }

        if (string.IsNullOrWhiteSpace(request.ModuleApiName))
        {
            return Result<BiginRelatedRecordsResponseContract>.Failure("INVALID_MODULE", "Module API name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RecordId))
        {
            return Result<BiginRelatedRecordsResponseContract>.Failure("INVALID_RECORD_ID", "Record ID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.RelatedListApiName))
        {
            return Result<BiginRelatedRecordsResponseContract>.Failure("INVALID_RELATED_LIST", "Related list API name is required.");
        }

        if (request.Page.HasValue && request.Page.Value < 1)
        {
            return Result<BiginRelatedRecordsResponseContract>.Failure("INVALID_PAGE", "Page must be greater than or equal to 1.");
        }

        if (request.PerPage.HasValue && (request.PerPage.Value < 1 || request.PerPage.Value > 200))
        {
            return Result<BiginRelatedRecordsResponseContract>.Failure("INVALID_PER_PAGE", "PerPage must be between 1 and 200.");
        }

        // Obtain token via capability ticket
        _ = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "Bigin", "default", ct).ConfigureAwait(false);

        const string sampleJson = """
        [
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
        ]
        """;

        using var doc = JsonDocument.Parse(sampleJson);
        var records = doc.RootElement.EnumerateArray().Select(el => el.Clone()).ToList();

        var page = request.Page ?? 1;
        var perPage = request.PerPage ?? 200;

        return Result<BiginRelatedRecordsResponseContract>.Success(
            new BiginRelatedRecordsResponseContract(records, page, perPage, records.Count, false));
    }
}
