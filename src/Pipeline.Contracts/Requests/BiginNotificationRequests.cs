namespace Pipeline.Contracts.Requests;

public sealed record BiginChannelSubscription(
    string ChannelId,
    IReadOnlyList<string>? Events,
    string? NotifyUrl,
    string? Token = null,
    string? ChannelExpiry = null,
    bool? DeleteEvents = null);

public sealed record EnableBiginNotificationsRequest(
    string DataCenter,
    IReadOnlyList<BiginChannelSubscription> Channels);

public sealed record UpdateBiginNotificationsRequest(
    string DataCenter,
    IReadOnlyList<BiginChannelSubscription> Channels);

public sealed record UpdateSpecificBiginNotificationsRequest(
    string DataCenter,
    IReadOnlyList<BiginChannelSubscription> Channels);

public sealed record DisableBiginNotificationsRequest(
    string DataCenter,
    IReadOnlyList<string> ChannelIds);

public sealed record DisableSpecificBiginNotificationsRequest(
    string DataCenter,
    IReadOnlyList<BiginChannelSubscription> Channels);

public sealed record GetBiginNotificationDetailsRequest(
    string DataCenter,
    string? ChannelId = null,
    string? Module = null,
    int? Page = null,
    int? PerPage = null);
