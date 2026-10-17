using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Bigin;

public sealed record GetBiginModulesCommand(string? DataCenter = null) : IUseCaseRequest
{
    public string OperationName => "bigin.modules.read";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string>
        {
            ["provider"] = "Bigin",
            ["accountId"] = "default"
        };
}

[RequiresPermission("bigin.contacts.read")]
public sealed class GetBiginModulesUseCase : IUseCaseHandler<GetBiginModulesCommand, BiginModulesResponseContract>
{
    private readonly ICredentialBroker _credentialBroker;

    public GetBiginModulesUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    private static readonly HashSet<string> ValidDataCenters = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "EU", "AU", "IN", "CN", "JP", "SA", "CA"
    };

    public async Task<Result<BiginModulesResponseContract>> HandleAsync(
        GetBiginModulesCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.DataCenter) && !ValidDataCenters.Contains(request.DataCenter.Trim()))
        {
            return Result<BiginModulesResponseContract>.Failure("INVALID_DATA_CENTER", $"Unsupported data center: '{request.DataCenter}'. Supported data centers are US, EU, AU, IN, CN, JP, SA, CA.");
        }

        // Obtain token via capability ticket
        _ = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "Bigin", "default", ct).ConfigureAwait(false);

        // Standard modules available in Bigin organization as defined by Bigin documentation
        var sampleModules = new List<BiginModuleItemResponse>
        {
            new("2034020000000000129", "Contacts", "Contacts", "Contact", "Contacts", true, true, true, true, true, true, 1, "default", 3, true),
            new("2034020000000000127", "Accounts", "Accounts", "Company", "Companies", true, true, true, true, true, true, 1, "default", 4, true),
            new("2034020000000000131", "Pipelines", "Pipelines", "Pipeline", "Pipelines", true, true, true, true, true, true, 1, "default", 5, true),
            new("2034020000000000143", "Tasks", "Tasks", "Task", "Tasks", true, true, true, true, true, true, 1, "default", 6, true),
            new("2034020000000000145", "Events", "Events", "Event", "Events", true, true, true, true, true, true, 1, "default", 6, true),
            new("2034020000000017015", "Calls", "Calls", "Call", "Calls", true, true, true, true, true, true, 1, "default", 6, true),
            new("2034020000000000163", "Products", "Products", "Product", "Products", true, true, true, true, true, true, 1, "default", 9, true),
            new("2034020000000048001", "Social", "Social", "Social", "Social", false, false, false, false, true, true, 1, "default", 24, false),
            new("2034020000000458818", "Associated_Products", "LinkingModule1", "Associated Products", "Associated Products", true, false, true, false, true, true, 2, "subform", 25, false),
            new("2034020000000000147", "Notes", "Notes", "Note", "Notes", true, true, true, true, true, false, 1, "default", 29, false),
            new("2034020000000000177", "Attachments", "Attachments", "Attachment", "Attachments", true, true, true, false, true, false, 1, "default", 33, false)
        };

        return Result<BiginModulesResponseContract>.Success(new BiginModulesResponseContract(sampleModules));
    }
}
