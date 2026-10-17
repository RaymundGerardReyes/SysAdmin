namespace Pipeline.Contracts.Responses;

public sealed record BiginEventSubscriptionResponse(
    string? ChannelExpiry,
    string? ResourceUri,
    string? ResourceId,
    string? ResourceName,
    string? ChannelId);

public sealed record BiginWatchChannelResponse(
    string Code,
    string Status,
    string Message,
    IReadOnlyList<BiginEventSubscriptionResponse>? Events,
    string? ChannelId,
    string? ResourceUri,
    string? ResourceId);

public sealed record BiginWatchOperationResponse(
    IReadOnlyList<BiginWatchChannelResponse> Watch);

public sealed record BiginNotificationDetailsContract(
    string ChannelId,
    string? ResourceName,
    string? ResourceUri,
    string? NotifyUrl,
    IReadOnlyList<string>? Events,
    string? ChannelExpiry,
    string? Token);

public sealed record BiginNotificationPageInfoContract(
    int PerPage,
    int Count,
    int Page,
    bool MoreRecords);

public sealed record GetBiginNotificationDetailsResponseContract(
    IReadOnlyList<BiginNotificationDetailsContract> Watch,
    BiginNotificationPageInfoContract Info);
