using System.Text.Json;
using System.Text.Json.Serialization;

namespace Integrations.Zoho.Bigin.Models;

/// <summary>
/// Channel subscription parameters in a Bigin watch request.
/// </summary>
public sealed record BiginWatchChannelRequest
{
    [JsonPropertyName("channel_id")]
    public required string ChannelId { get; init; }

    [JsonPropertyName("events")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Events { get; init; }

    [JsonPropertyName("notify_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NotifyUrl { get; init; }

    [JsonPropertyName("token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Token { get; init; }

    [JsonPropertyName("channel_expiry")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ChannelExpiry { get; init; }

    [JsonPropertyName("_delete_events")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? DeleteEvents { get; init; }

    /// <summary>
    /// Validates Enable notification requirements: channel_id, events (non-empty), notify_url, token length <= 50, and valid channel_expiry if set.
    /// </summary>
    public void ValidateForEnable()
    {
        if (string.IsNullOrWhiteSpace(ChannelId))
        {
            throw new ArgumentException("ChannelId is required for enabling notifications.");
        }

        if (Events is null || Events.Count == 0 || Events.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one non-empty event is required for enabling notifications.");
        }

        if (string.IsNullOrWhiteSpace(NotifyUrl) || !Uri.TryCreate(NotifyUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            throw new ArgumentException($"A valid absolute notify_url is required for enabling notifications. Received '{NotifyUrl}'.");
        }

        if (Token is not null && Token.Length > 50)
        {
            throw new ArgumentException($"Token must not exceed 50 characters (Zoho Bigin constraint). Got {Token.Length}.");
        }

        ValidateChannelExpiry();
    }

    /// <summary>
    /// Validates Update notification requirements: channel_id, notify_url, token length <= 50, and valid channel_expiry if set.
    /// </summary>
    public void ValidateForUpdate()
    {
        if (string.IsNullOrWhiteSpace(ChannelId))
        {
            throw new ArgumentException("ChannelId is required for updating notifications.");
        }

        if (string.IsNullOrWhiteSpace(NotifyUrl) || !Uri.TryCreate(NotifyUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
        {
            throw new ArgumentException($"A valid absolute notify_url is required for updating notifications. Received '{NotifyUrl}'.");
        }

        if (Token is not null && Token.Length > 50)
        {
            throw new ArgumentException($"Token must not exceed 50 characters (Zoho Bigin constraint). Got {Token.Length}.");
        }

        if (Events is not null && Events.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Events must not contain empty or whitespace strings.");
        }

        ValidateChannelExpiry();
    }

    /// <summary>
    /// Validates Specific Update notification requirements: channel_id is required, token length <= 50, and valid channel_expiry if set.
    /// </summary>
    public void ValidateForSpecificUpdate()
    {
        if (string.IsNullOrWhiteSpace(ChannelId))
        {
            throw new ArgumentException("ChannelId is required for updating notification details.");
        }

        if (NotifyUrl is not null && (!Uri.TryCreate(NotifyUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")))
        {
            throw new ArgumentException($"NotifyUrl must be a valid absolute URL when specified. Received '{NotifyUrl}'.");
        }

        if (Token is not null && Token.Length > 50)
        {
            throw new ArgumentException($"Token must not exceed 50 characters (Zoho Bigin constraint). Got {Token.Length}.");
        }

        if (Events is not null && Events.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Events must not contain empty or whitespace strings.");
        }

        ValidateChannelExpiry();
    }

    /// <summary>
    /// Validates Disable Specific Events requirements: channel_id, non-empty events, and _delete_events = true.
    /// </summary>
    public void ValidateForDisableSpecific()
    {
        if (string.IsNullOrWhiteSpace(ChannelId))
        {
            throw new ArgumentException("ChannelId is required for disabling specific events.");
        }

        if (Events is null || Events.Count == 0 || Events.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one non-empty event is required when disabling specific events.");
        }

        if (DeleteEvents != true)
        {
            throw new ArgumentException("_delete_events must be true when disabling specific notifications.");
        }
    }

    private void ValidateChannelExpiry()
    {
        if (!string.IsNullOrWhiteSpace(ChannelExpiry) && !DateTimeOffset.TryParse(ChannelExpiry, out _))
        {
            throw new ArgumentException($"channel_expiry must be a valid ISO8601 datetime string. Received '{ChannelExpiry}'.");
        }
    }
}

/// <summary>
/// Envelope request body for watch operations containing up to 100 channels.
/// </summary>
public sealed record BiginWatchRequestBody
{
    public const int MaxChannelsPerCall = 100;

    [JsonPropertyName("watch")]
    public required IReadOnlyList<BiginWatchChannelRequest> Watch { get; init; }

    public void Validate()
    {
        if (Watch is null || Watch.Count == 0)
        {
            throw new ArgumentException("Watch channels list must contain at least one channel.");
        }

        if (Watch.Count > MaxChannelsPerCall)
        {
            throw new ArgumentException($"Cannot configure more than {MaxChannelsPerCall} channels in a single API call.");
        }
    }
}

/// <summary>
/// Summary of an individual event subscription returned by Bigin.
/// </summary>
public sealed record BiginWatchEventSummary
{
    [JsonPropertyName("channel_expiry")]
    public string? ChannelExpiry { get; init; }

    [JsonPropertyName("resource_uri")]
    public string? ResourceUri { get; init; }

    [JsonPropertyName("resource_id")]
    public string? ResourceId { get; init; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; init; }

    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; init; }
}

/// <summary>
/// Details container inside a watch response item.
/// </summary>
public sealed record BiginWatchChannelDetails
{
    [JsonPropertyName("events")]
    public IReadOnlyList<BiginWatchEventSummary>? Events { get; init; }

    [JsonPropertyName("resource_uri")]
    public string? ResourceUri { get; init; }

    [JsonPropertyName("resource_id")]
    public string? ResourceId { get; init; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; init; }

    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; init; }

    [JsonPropertyName("api_name")]
    public string? ApiName { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

/// <summary>
/// Per-channel result item in watch response.
/// </summary>
public sealed record BiginWatchChannelResult
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("details")]
    public BiginWatchChannelDetails? Details { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    public bool IsSuccess => string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(Code, "SUCCESS", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Envelope response returned by Bigin watch operations.
/// </summary>
public sealed record BiginWatchResponse
{
    [JsonPropertyName("watch")]
    public required IReadOnlyList<BiginWatchChannelResult> Watch { get; init; }

    public bool AllSucceeded => Watch is not null && Watch.Count > 0 && Watch.All(r => r.IsSuccess);
}
