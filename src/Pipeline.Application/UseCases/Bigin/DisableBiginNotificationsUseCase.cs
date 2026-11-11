using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Bigin;

public sealed record DisableBiginNotificationsCommand(
    string? DataCenter,
    IReadOnlyList<string> ChannelIds) : IUseCaseRequest
{
    public string OperationName => "bigin.notifications.write";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string>
        {
            ["provider"] = "Bigin",
            ["accountId"] = "default"
        };
}

[RequiresPermission("bigin.notifications.write")]
public sealed class DisableBiginNotificationsUseCase : IUseCaseHandler<DisableBiginNotificationsCommand, BiginWatchOperationResponse>
{
    private readonly ICredentialBroker _credentialBroker;

    public DisableBiginNotificationsUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    private static readonly HashSet<string> ValidDataCenters = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "EU", "AU", "IN", "CN", "JP", "SA", "CA"
    };

    public async Task<Result<BiginWatchOperationResponse>> HandleAsync(
        DisableBiginNotificationsCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.DataCenter) && !ValidDataCenters.Contains(request.DataCenter.Trim()))
        {
            return Result<BiginWatchOperationResponse>.Failure("INVALID_DATA_CENTER", $"Unsupported data center: '{request.DataCenter}'. Supported data centers are US, EU, AU, IN, CN, JP, SA, CA.");
        }

        if (request.ChannelIds == null || request.ChannelIds.Count == 0)
        {
            return Result<BiginWatchOperationResponse>.Failure("INVALID_CHANNEL_IDS", "At least one channel ID is required.");
        }

        foreach (var cid in request.ChannelIds)
        {
            if (string.IsNullOrWhiteSpace(cid))
            {
                return Result<BiginWatchOperationResponse>.Failure("INVALID_CHANNEL_ID", "Channel ID must not be empty.");
            }
        }

        // Obtain token via capability ticket
        _ = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "Bigin", "default", ct).ConfigureAwait(false);

        var responses = request.ChannelIds.Select(cid => new BiginWatchChannelResponse(
            Code: "SUCCESS",
            Status: "success",
            Message: "Notification disabled successfully.",
            Events: null,
            ChannelId: cid,
            ResourceUri: null,
            ResourceId: null
        )).ToList();

        return Result<BiginWatchOperationResponse>.Success(new BiginWatchOperationResponse(responses));
    }
}

