using System.Text.Json;
using System.Text.Json.Serialization;

namespace Integrations.Zoho.Bigin.Models;

/// <summary>
/// Response object for GET /bigin/v2/{module_api_name}/{record_id}/{related_list_api_name}.
/// </summary>
public sealed record BiginRelatedListResponse(
    [property: JsonPropertyName("data")] IReadOnlyList<JsonElement>? Data,
    [property: JsonPropertyName("info")] BiginRelatedListInfo? Info
)
{
    public IReadOnlyList<JsonElement> Records => Data ?? Array.Empty<JsonElement>();
}

/// <summary>
/// Pagination and cursor info for related list responses.
/// </summary>
public sealed record BiginRelatedListInfo(
    [property: JsonPropertyName("per_page")] int? PerPage,
    [property: JsonPropertyName("next_page_token")] string? NextPageToken,
    [property: JsonPropertyName("count")] int? Count,
    [property: JsonPropertyName("page")] int? Page,
    [property: JsonPropertyName("previous_page_token")] string? PreviousPageToken,
    [property: JsonPropertyName("page_token_expiry")] string? PageTokenExpiry,
    [property: JsonPropertyName("more_records")] bool? MoreRecords
)
{
    public int PageIndex => Page ?? 1;
    public int RecordsPerPage => PerPage ?? 200;
    public int RecordCount => Count ?? 0;
    public bool HasMoreRecords => MoreRecords ?? false;
}

/// <summary>
/// Query parameters for fetching related list records.
/// </summary>
public sealed record BiginRelatedListQuery
{
    /// <summary>
    /// Comma-separated field API names (e.g. "Deal_Name,Sub_Pipeline,Stage,Pipeline").
    /// Note: The Emails related list currently does not support the fields query parameter.
    /// </summary>
    public IReadOnlyList<string>? Fields { get; init; }

    /// <summary>
    /// 1-based page index. Default value is 1.
    /// </summary>
    public int? Page { get; init; }

    /// <summary>
    /// Number of records per page. Maximum allowed is 200.
    /// </summary>
    public int? PerPage { get; init; }

    public void Validate(string? relatedListApiName = null)
    {
        if (Page.HasValue && Page.Value < 1)
        {
            throw new ArgumentOutOfRangeException(paramName: null, message: "Page must be greater than or equal to 1.");
        }

        if (PerPage.HasValue && (PerPage.Value < 1 || PerPage.Value > 200))
        {
            throw new ArgumentOutOfRangeException(paramName: null, message: "PerPage must be between 1 and 200 (Bigin maximum is 200 records per call).");
        }
    }

    public string ToQueryString(string? relatedListApiName = null)
    {
        var parameters = new List<string>();

        // Note from documentation: The Emails related list currently does not support the fields query parameter.
        var isEmails = string.Equals(relatedListApiName, "Emails", StringComparison.OrdinalIgnoreCase);

        if (!isEmails && Fields is not null && Fields.Count > 0)
        {
            var validFields = Fields.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.Trim());
            var joined = string.Join(",", validFields);
            if (!string.IsNullOrEmpty(joined))
            {
                parameters.Add($"fields={Uri.EscapeDataString(joined)}");
            }
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
