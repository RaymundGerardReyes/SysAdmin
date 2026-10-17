using System.Text.Json;
using System.Text.Json.Serialization;

namespace Integrations.Zoho.Bigin.Models;

/// <summary>
/// Query parameters for fetching enabled notification details.
/// </summary>
public sealed record BiginNotificationDetailsQuery
{
    public const int MaxPerPage = 200;

    public string? ChannelId { get; init; }
    public string? Module { get; init; }
    public int? Page { get; init; }
    public int? PerPage { get; init; }

    public void Validate()
    {
        if (Page.HasValue && Page.Value < 1)
        {
            throw new ArgumentOutOfRangeException(paramName: null, message: "Page index must be greater than or equal to 1.");
        }

        if (PerPage.HasValue && (PerPage.Value < 1 || PerPage.Value > MaxPerPage))
        {
            throw new ArgumentOutOfRangeException(paramName: null, message: $"PerPage must be between 1 and {MaxPerPage}.");
        }

        if (Module is not null && !BiginModules.IsKnownModule(Module))
        {
            throw new ArgumentException($"Unknown Bigin module '{Module}'. Allowed: {string.Join(", ", BiginModules.All)}.");
        }
    }

    public string ToQueryString()
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(ChannelId))
        {
            parameters.Add($"channel_id={Uri.EscapeDataString(ChannelId.Trim())}");
        }

        if (!string.IsNullOrWhiteSpace(Module))
        {
            var normalizedModule = BiginModules.NormalizeModuleApiName(Module);
            parameters.Add($"module={Uri.EscapeDataString(normalizedModule)}");
        }

        if (Page.HasValue)
        {
            parameters.Add($"page={Page.Value}");
        }

        if (PerPage.HasValue)
        {
            parameters.Add($"per_page={PerPage.Value}");
        }

        return parameters.Count > 0 ? "?" + string.Join("&", parameters) : string.Empty;
    }
}

/// <summary>
/// Individual notification detail item returned from GET /actions/watch.
/// </summary>
public sealed record BiginNotificationDetailItem
{
    [JsonPropertyName("notify_on_related_action")]
    public bool NotifyOnRelatedAction { get; init; }

    [JsonPropertyName("channel_expiry")]
    public string? ChannelExpiry { get; init; }

    [JsonPropertyName("return_affected_field_values")]
    public bool ReturnAffectedFieldValues { get; init; }

    [JsonPropertyName("resource_uri")]
    public string? ResourceUri { get; init; }

    [JsonPropertyName("resource_id")]
    public string? ResourceId { get; init; }

    [JsonPropertyName("notify_url")]
    public string? NotifyUrl { get; init; }

    [JsonPropertyName("resource_name")]
    public string? ResourceName { get; init; }

    [JsonPropertyName("fields")]
    public JsonElement? Fields { get; init; }

    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; init; }

    [JsonPropertyName("events")]
    public IReadOnlyList<string>? Events { get; init; }

    [JsonPropertyName("token")]
    public string? Token { get; init; }
}

/// <summary>
/// Pagination info returned from GET /actions/watch.
/// </summary>
public sealed record BiginNotificationPageInfo
{
    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    [JsonPropertyName("count")]
    public int Count { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("more_records")]
    public bool MoreRecords { get; init; }
}

/// <summary>
/// Envelope response returned from GET /actions/watch.
/// </summary>
public sealed record BiginGetNotificationDetailsResponse
{
    [JsonPropertyName("watch")]
    public required IReadOnlyList<BiginNotificationDetailItem> Watch { get; init; }

    [JsonPropertyName("info")]
    public required BiginNotificationPageInfo Info { get; init; }
}
