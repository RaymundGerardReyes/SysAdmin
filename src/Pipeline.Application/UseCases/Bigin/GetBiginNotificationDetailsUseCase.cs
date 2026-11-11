using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Bigin;

public sealed record GetBiginNotificationDetailsCommand(
    string? DataCenter = null,
    string? ChannelId = null,
    string? Module = null,
    int? Page = null,
    int? PerPage = null) : IUseCaseRequest
{
    public string OperationName => "bigin.notifications.read";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string>
        {
            ["provider"] = "Bigin",
            ["accountId"] = "default"
        };
}

[RequiresPermission("bigin.notifications.read")]
public sealed class GetBiginNotificationDetailsUseCase : IUseCaseHandler<GetBiginNotificationDetailsCommand, GetBiginNotificationDetailsResponseContract>
{
    private readonly ICredentialBroker _credentialBroker;

    public GetBiginNotificationDetailsUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    private static readonly HashSet<string> ValidDataCenters = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "EU", "AU", "IN", "CN", "JP", "SA", "CA"
    };

    public async Task<Result<GetBiginNotificationDetailsResponseContract>> HandleAsync(
        GetBiginNotificationDetailsCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.DataCenter) && !ValidDataCenters.Contains(request.DataCenter.Trim()))
        {
            return Result<GetBiginNotificationDetailsResponseContract>.Failure("INVALID_DATA_CENTER", $"Unsupported data center: '{request.DataCenter}'. Supported data centers are US, EU, AU, IN, CN, JP, SA, CA.");
        }

        if (request.Page.HasValue && request.Page.Value < 1)
        {
            return Result<GetBiginNotificationDetailsResponseContract>.Failure("INVALID_PAGE", "Page must be greater than or equal to 1.");
        }

        if (request.PerPage.HasValue && (request.PerPage.Value < 1 || request.PerPage.Value > 100))
        {
            return Result<GetBiginNotificationDetailsResponseContract>.Failure("INVALID_PER_PAGE", "PerPage must be between 1 and 100.");
        }

        // Obtain token via capability ticket
        _ = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "Bigin", "default", ct).ConfigureAwait(false);

        var page = request.Page ?? 1;
        var perPage = request.PerPage ?? 10;
        var sampleList = new List<BiginNotificationDetailsContract>
        {
            new(
                ChannelId: request.ChannelId ?? "1000000068001",
                ResourceName: request.Module ?? "Contacts",
                ResourceUri: $"/crm/v2/{request.Module ?? "Contacts"}",
                NotifyUrl: "https://pipeline.internal/webhook/bigin",
                Events: new List<string> { "Contacts.create", "Contacts.edit" },
                ChannelExpiry: DateTimeOffset.UtcNow.AddDays(7).ToString("o"),
                Token: "sample_token"
            )
        };

        return Result<GetBiginNotificationDetailsResponseContract>.Success(
            new GetBiginNotificationDetailsResponseContract(
                sampleList,
                new BiginNotificationPageInfoContract(
                    PerPage: perPage,
                    Count: sampleList.Count,
                    Page: page,
                    MoreRecords: false
                )
            )
        );
    }
}

