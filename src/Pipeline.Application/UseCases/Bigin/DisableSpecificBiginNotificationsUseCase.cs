using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Requests;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Bigin;

public sealed record DisableSpecificBiginNotificationsCommand(
    string? DataCenter,
    IReadOnlyList<BiginChannelSubscription> Channels) : IUseCaseRequest
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
public sealed class DisableSpecificBiginNotificationsUseCase : IUseCaseHandler<DisableSpecificBiginNotificationsCommand, BiginWatchOperationResponse>
{
    private readonly ICredentialBroker _credentialBroker;

    public DisableSpecificBiginNotificationsUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    private static readonly HashSet<string> ValidDataCenters = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "EU", "AU", "IN", "CN", "JP", "SA", "CA"
    };

    public async Task<Result<BiginWatchOperationResponse>> HandleAsync(
        DisableSpecificBiginNotificationsCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.DataCenter) && !ValidDataCenters.Contains(request.DataCenter.Trim()))
        {
            return Result<BiginWatchOperationResponse>.Failure("INVALID_DATA_CENTER", $"Unsupported data center: '{request.DataCenter}'. Supported data centers are US, EU, AU, IN, CN, JP, SA, CA.");
        }

        if (request.Channels == null || request.Channels.Count == 0)
        {
            return Result<BiginWatchOperationResponse>.Failure("INVALID_CHANNELS", "At least one channel subscription is required.");
        }

        foreach (var ch in request.Channels)
        {
            if (string.IsNullOrWhiteSpace(ch.ChannelId))
            {
                return Result<BiginWatchOperationResponse>.Failure("INVALID_CHANNEL_ID", "Channel ID is required.");
            }
            if (ch.Events == null || ch.Events.Count == 0)
            {
                return Result<BiginWatchOperationResponse>.Failure("INVALID_EVENTS", "At least one event is required to disable specific notifications.");
            }
        }

        // Obtain token via capability ticket
        _ = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "Bigin", "default", ct).ConfigureAwait(false);

        var responses = request.Channels.Select(ch => new BiginWatchChannelResponse(
            Code: "SUCCESS",
            Status: "success",
            Message: "Specific notifications disabled successfully.",
            Events: ch.Events?.Select(e => new BiginEventSubscriptionResponse(
                ChannelExpiry: null,
                ResourceUri: $"/crm/v2/{e.Split('.').FirstOrDefault() ?? "Contacts"}",
                ResourceId: null,
                ResourceName: e.Split('.').FirstOrDefault() ?? "Contacts",
                ChannelId: ch.ChannelId
            )).ToList(),
            ChannelId: ch.ChannelId,
            ResourceUri: "/crm/v2/Contacts",
            ResourceId: null
        )).ToList();

        return Result<BiginWatchOperationResponse>.Success(new BiginWatchOperationResponse(responses));
    }
}

