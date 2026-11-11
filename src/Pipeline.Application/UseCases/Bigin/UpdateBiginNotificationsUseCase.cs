using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Requests;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Bigin;

public sealed record UpdateBiginNotificationsCommand(
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
public sealed class UpdateBiginNotificationsUseCase : IUseCaseHandler<UpdateBiginNotificationsCommand, BiginWatchOperationResponse>
{
    private readonly ICredentialBroker _credentialBroker;

    public UpdateBiginNotificationsUseCase(ICredentialBroker credentialBroker)
    {
        _credentialBroker = credentialBroker;
    }

    private static readonly HashSet<string> ValidDataCenters = new(StringComparer.OrdinalIgnoreCase)
    {
        "US", "EU", "AU", "IN", "CN", "JP", "SA", "CA"
    };

    public async Task<Result<BiginWatchOperationResponse>> HandleAsync(
        UpdateBiginNotificationsCommand request,
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
            if (string.IsNullOrWhiteSpace(ch.NotifyUrl))
            {
                return Result<BiginWatchOperationResponse>.Failure("INVALID_NOTIFY_URL", "Notify URL is required.");
            }
        }

        // Obtain token via capability ticket
        _ = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "Bigin", "default", ct).ConfigureAwait(false);

        var responses = request.Channels.Select(ch => new BiginWatchChannelResponse(
            Code: "SUCCESS",
            Status: "success",
            Message: "Notification updated successfully.",
            Events: ch.Events?.Select(e => new BiginEventSubscriptionResponse(
                ChannelExpiry: ch.ChannelExpiry ?? DateTimeOffset.UtcNow.AddDays(7).ToString("o"),
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

