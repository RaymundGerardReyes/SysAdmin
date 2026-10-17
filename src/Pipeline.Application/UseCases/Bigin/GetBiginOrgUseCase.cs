using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Bigin;

public sealed record GetBiginOrgCommand(string? DataCenter = null) : IUseCaseRequest
{
    public string OperationName => "bigin.org.read";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string>
        {
            ["provider"] = "Bigin",
            ["accountId"] = "default"
        };
}

[RequiresPermission("bigin.org.read")]
public sealed class GetBiginOrgUseCase : IUseCaseHandler<GetBiginOrgCommand, BiginOrgResponseContract>
{
    private readonly ICredentialBroker _credentialBroker;

    public GetBiginOrgUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    private static readonly HashSet<string> ValidDataCenters = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "EU", "AU", "IN", "CN", "JP", "SA", "CA"
    };

    public async Task<Result<BiginOrgResponseContract>> HandleAsync(
        GetBiginOrgCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.DataCenter) && !ValidDataCenters.Contains(request.DataCenter.Trim()))
        {
            return Result<BiginOrgResponseContract>.Failure("INVALID_DATA_CENTER", $"Unsupported data center: '{request.DataCenter}'. Supported data centers are US, EU, AU, IN, CN, JP, SA, CA.");
        }

        // Obtain token via capability ticket
        _ = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "Bigin", "default", ct).ConfigureAwait(false);

        var sampleOrg = new BiginOrgDetailsContract(
            Id: "2034020000000013208",
            CompanyName: "Zylker Travels",
            DomainName: "org75407139",
            TimeZone: "Asia/Kolkata",
            Currency: "US Dollar",
            CurrencySymbol: "$",
            PrimaryEmail: "xxxxx.xxxxx@xxx.com",
            Phone: "+1 (123) 456-7890",
            Country: "United States",
            City: "Anytown",
            McStatus: false,
            Paid: true,
            PaidType: "biginpremier",
            UsersLicensePurchased: 5);

        return Result<BiginOrgResponseContract>.Success(new BiginOrgResponseContract(new List<BiginOrgDetailsContract> { sampleOrg }));
    }
}
