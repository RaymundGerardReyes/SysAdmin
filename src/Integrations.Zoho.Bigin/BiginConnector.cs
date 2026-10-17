using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Integrations.Zoho.Bigin.Models;

namespace Integrations.Zoho.Bigin;

/// <summary>
/// Connector implementation for Zoho Bigin v2 APIs (Modules and Notifications) across 8 Data Centers.
/// Kept internal to prevent anti-bypass violations (Rule C1).
/// </summary>
internal sealed class BiginConnector : IBiginConnector
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;

    public BiginConnector(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Legacy raw contacts fetch.
    /// </summary>
    public async Task<string> GetContactsRawAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("https://bigin.zoho.com/api/v1/Contacts", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetModulesRawAsync(
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default)
    {
        var url = dataCenter.GetModulesEndpointUrl();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.TryAddWithoutValidation("Authorization", $"Zoho-oauthtoken {accessToken}");
        }

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw BiginApiException.FromHttpResponse(response.StatusCode, content, url);
        }

        return content;
    }

    /// <inheritdoc />
    public async Task<BiginModulesResponse> GetModulesAsync(
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default)
    {
        var json = await GetModulesRawAsync(dataCenter, accessToken, ct).ConfigureAwait(false);
        var parsed = JsonSerializer.Deserialize<BiginModulesResponse>(json, DefaultJsonOptions);

        if (parsed is null || parsed.Modules is null)
        {
            throw new JsonException("Deserialized BiginModulesResponse payload is null or missing modules array.");
        }

        return parsed;
    }

    /// <inheritdoc />
    public async Task<BiginModule?> GetModuleByApiNameAsync(
        string apiName,
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName);
        var normalized = BiginApiNameValidator.NormalizeApiName(apiName);
        var response = await GetModulesAsync(dataCenter, accessToken, ct).ConfigureAwait(false);
        return response.Modules.FirstOrDefault(m =>
            string.Equals(m.ApiName, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.ApiName, apiName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.ModuleName, apiName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.PluralLabel, apiName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.SingularLabel, apiName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds a specific module by its unique ID.
    /// </summary>
    public async Task<BiginModule?> GetModuleByIdAsync(
        string id,
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var response = await GetModulesAsync(dataCenter, accessToken, ct).ConfigureAwait(false);
        return response.Modules.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Retrieves all default system modules.
    /// </summary>
    public async Task<IReadOnlyList<BiginModule>> GetDefaultModulesAsync(
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default)
    {
        var response = await GetModulesAsync(dataCenter, accessToken, ct).ConfigureAwait(false);
        return response.Modules
            .Where(m => string.Equals(m.GeneratedType, BiginGeneratedTypes.Default, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Retrieves all modules that support record creation via API.
    /// </summary>
    public async Task<IReadOnlyList<BiginModule>> GetCreatableModulesAsync(
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default)
    {
        var response = await GetModulesAsync(dataCenter, accessToken, ct).ConfigureAwait(false);
        return response.Modules
            .Where(m => m.Creatable && m.ApiSupported)
            .ToList();
    }

    // ==========================================
    // NOTIFICATIONS API (v2 /actions/watch)
    // ==========================================

    /// <inheritdoc />
    public async Task<BiginWatchResponse> EnableNotificationsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginWatchRequestBody request,
        CancellationToken ct = default)
    {
        ValidateAuthToken(accessToken);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        foreach (var channel in request.Watch)
        {
            channel.ValidateForEnable();
        }

        var endpoint = dc.GetEndpointUri("actions/watch");
        return await SendWatchPayloadAsync(HttpMethod.Post, endpoint, accessToken, request, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BiginGetNotificationDetailsResponse> GetNotificationDetailsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginNotificationDetailsQuery? query = null,
        CancellationToken ct = default)
    {
        ValidateAuthToken(accessToken);
        query?.Validate();

        var queryString = query?.ToQueryString() ?? string.Empty;
        var endpoint = new Uri(dc.GetEndpointUri("actions/watch").ToString() + queryString);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, endpoint);
        AddZohoAuthHeader(requestMessage, accessToken);

        using var response = await _httpClient.SendAsync(requestMessage, ct).ConfigureAwait(false);
        var rawBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw BiginApiException.FromHttpResponse(response.StatusCode, rawBody, endpoint.ToString());
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<BiginGetNotificationDetailsResponse>(rawBody, DefaultJsonOptions);
            if (parsed is null || parsed.Watch is null)
            {
                throw BiginApiException.FromHttpResponse(response.StatusCode, rawBody, endpoint.ToString());
            }

            return parsed;
        }
        catch (JsonException ex)
        {
            throw new BiginApiException(response.StatusCode, $"Invalid JSON returned by Bigin: {ex.Message}")
            {
                RawResponseBody = rawBody,
                RequestUrl = endpoint.ToString()
            };
        }
    }

    /// <inheritdoc />
    public async Task<BiginWatchResponse> UpdateNotificationDetailsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginWatchRequestBody request,
        CancellationToken ct = default)
    {
        ValidateAuthToken(accessToken);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        foreach (var channel in request.Watch)
        {
            channel.ValidateForUpdate();
        }

        var endpoint = dc.GetEndpointUri("actions/watch");
        return await SendWatchPayloadAsync(HttpMethod.Put, endpoint, accessToken, request, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BiginWatchResponse> UpdateSpecificNotificationDetailsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginWatchRequestBody request,
        CancellationToken ct = default)
    {
        ValidateAuthToken(accessToken);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        foreach (var channel in request.Watch)
        {
            channel.ValidateForSpecificUpdate();
        }

        var endpoint = dc.GetEndpointUri("actions/watch");
        return await SendWatchPayloadAsync(HttpMethod.Patch, endpoint, accessToken, request, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BiginWatchResponse> DisableNotificationsAsync(
        BiginDataCenter dc,
        string accessToken,
        IEnumerable<string> channelIds,
        CancellationToken ct = default)
    {
        ValidateAuthToken(accessToken);
        ArgumentNullException.ThrowIfNull(channelIds);

        var idList = channelIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id.Trim()).Distinct().ToList();
        if (idList.Count == 0)
        {
            throw new ArgumentException("At least one valid channel_id is required to disable notifications.", nameof(channelIds));
        }

        if (idList.Count > BiginWatchRequestBody.MaxChannelsPerCall)
        {
            throw new ArgumentException($"Cannot disable more than {BiginWatchRequestBody.MaxChannelsPerCall} channels in a single API call.", nameof(channelIds));
        }

        var commaSeparated = string.Join(",", idList);
        var endpoint = new Uri(dc.GetEndpointUri("actions/watch").ToString() + $"?channel_ids={Uri.EscapeDataString(commaSeparated)}");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        AddZohoAuthHeader(requestMessage, accessToken);

        using var response = await _httpClient.SendAsync(requestMessage, ct).ConfigureAwait(false);
        var rawBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw BiginApiException.FromHttpResponse(response.StatusCode, rawBody, endpoint.ToString());
        }

        return ParseWatchResponse(response.StatusCode, rawBody, endpoint.ToString());
    }

    /// <inheritdoc />
    public async Task<BiginWatchResponse> DisableSpecificNotificationsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginWatchRequestBody request,
        CancellationToken ct = default)
    {
        ValidateAuthToken(accessToken);
        ArgumentNullException.ThrowIfNull(request);
        request.Validate();

        var normalizedChannels = new List<BiginWatchChannelRequest>(request.Watch.Count);
        foreach (var channel in request.Watch)
        {
            var effectiveChannel = channel.DeleteEvents.HasValue
                ? channel
                : channel with { DeleteEvents = true };

            effectiveChannel.ValidateForDisableSpecific();
            normalizedChannels.Add(effectiveChannel);
        }

        var normalizedRequest = request with { Watch = normalizedChannels };

        var endpoint = dc.GetEndpointUri("actions/watch");
        return await SendWatchPayloadAsync(HttpMethod.Patch, endpoint, accessToken, normalizedRequest, ct).ConfigureAwait(false);
    }

    private async Task<BiginWatchResponse> SendWatchPayloadAsync(
        HttpMethod method,
        Uri endpoint,
        string accessToken,
        BiginWatchRequestBody payload,
        CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(payload, DefaultJsonOptions);
        using var requestMessage = new HttpRequestMessage(method, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        AddZohoAuthHeader(requestMessage, accessToken);

        using var response = await _httpClient.SendAsync(requestMessage, ct).ConfigureAwait(false);
        var rawBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw BiginApiException.FromHttpResponse(response.StatusCode, rawBody, endpoint.ToString());
        }

        return ParseWatchResponse(response.StatusCode, rawBody, endpoint.ToString());
    }

    private static BiginWatchResponse ParseWatchResponse(HttpStatusCode statusCode, string rawBody, string? requestUrl = null)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<BiginWatchResponse>(rawBody, DefaultJsonOptions);
            if (parsed is null || parsed.Watch is null || parsed.Watch.Count == 0)
            {
                throw BiginApiException.FromHttpResponse(statusCode, rawBody, requestUrl);
            }

            var firstFailure = parsed.Watch.FirstOrDefault(w => !w.IsSuccess);
            if (firstFailure is not null && !parsed.Watch.Any(w => w.IsSuccess))
            {
                throw BiginApiException.FromHttpResponse(statusCode, rawBody, requestUrl);
            }

            return parsed;
        }
        catch (JsonException ex)
        {
            throw new BiginApiException(statusCode, $"Failed to parse Bigin response: {ex.Message}")
            {
                RawResponseBody = rawBody,
                RequestUrl = requestUrl
            };
        }
    }

    private static void AddZohoAuthHeader(HttpRequestMessage request, string accessToken)
    {
        request.Headers.TryAddWithoutValidation("Authorization", $"Zoho-oauthtoken {accessToken}");
    }

    private static void ValidateAuthToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("Access token must not be null or whitespace.", nameof(accessToken));
        }
    }

    // ==========================================
    // ORGANIZATION API (v2 /org and /org/photo)
    // ==========================================

    public const long MaxPhotoSizeBytes = 1024 * 1024; // 1 MB (1,048,576 bytes)

    /// <inheritdoc />
    public async Task<string> GetOrgDetailsRawAsync(
        BiginDataCenter dc,
        string accessToken,
        CancellationToken ct = default)
    {
        ValidateAuthToken(accessToken);
        var url = dc.GetOrgEndpointUrl();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddZohoAuthHeader(request, accessToken);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw BiginApiException.FromHttpResponse(response.StatusCode, content, url);
        }

        return content;
    }

    /// <inheritdoc />
    public async Task<BiginOrgResponse> GetOrgDetailsAsync(
        BiginDataCenter dc,
        string accessToken,
        CancellationToken ct = default)
    {
        var json = await GetOrgDetailsRawAsync(dc, accessToken, ct).ConfigureAwait(false);
        var parsed = JsonSerializer.Deserialize<BiginOrgResponse>(json, DefaultJsonOptions);

        if (parsed is null || parsed.Org is null)
        {
            throw new JsonException("Deserialized BiginOrgResponse payload is null or missing org array.");
        }

        return parsed;
    }

    /// <inheritdoc />
    public async Task<BiginPhotoUploadResponse> UploadOrgPhotoAsync(
        BiginDataCenter dc,
        string accessToken,
        Stream fileStream,
        string fileName,
        string contentType = "image/png",
        CancellationToken ct = default)
    {
        ValidateAuthToken(accessToken);
        ArgumentNullException.ThrowIfNull(fileStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (!Path.HasExtension(fileName))
        {
            throw new ArgumentException("File name must include a valid extension (e.g. .png, .jpg).", nameof(fileName));
        }

        if (fileStream.CanSeek)
        {
            if (fileStream.Length == 0)
            {
                throw new ArgumentException("File cannot be empty.", nameof(fileStream));
            }

            if (fileStream.Length > MaxPhotoSizeBytes)
            {
                throw new ArgumentException(
                    $"File size ({fileStream.Length} bytes) exceeds the allowed limit of 1 MB ({MaxPhotoSizeBytes} bytes).",
                    nameof(fileStream));
            }

            if (fileStream.Position != 0)
            {
                fileStream.Position = 0;
            }
        }

        var url = dc.GetOrgPhotoEndpointUrl();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        content.Add(streamContent, "file", fileName);

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        AddZohoAuthHeader(requestMessage, accessToken);

        using var response = await _httpClient.SendAsync(requestMessage, ct).ConfigureAwait(false);
        var rawBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw BiginApiException.FromHttpResponse(response.StatusCode, rawBody, url);
        }

        var parsed = JsonSerializer.Deserialize<BiginPhotoUploadResponse>(rawBody, DefaultJsonOptions);
        if (parsed is null)
        {
            throw new BiginApiException(response.StatusCode, "Failed to parse photo upload response from Bigin.")
            {
                RawResponseBody = rawBody,
                RequestUrl = url
            };
        }

        if (!parsed.IsSuccess)
        {
            throw BiginApiException.FromHttpResponse(response.StatusCode, rawBody, url);
        }

        return parsed;
    }

    /// <inheritdoc />
    public async Task<BiginPhotoUploadResponse> UploadOrgPhotoAsync(
        BiginDataCenter dc,
        string accessToken,
        byte[] fileBytes,
        string fileName,
        string contentType = "image/png",
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fileBytes);
        if (fileBytes.Length == 0)
        {
            throw new ArgumentException("File cannot be empty.", nameof(fileBytes));
        }

        if (fileBytes.Length > MaxPhotoSizeBytes)
        {
            throw new ArgumentException(
                $"File size ({fileBytes.Length} bytes) exceeds the allowed limit of 1 MB ({MaxPhotoSizeBytes} bytes).",
                nameof(fileBytes));
        }

        using var stream = new MemoryStream(fileBytes);
        return await UploadOrgPhotoAsync(dc, accessToken, stream, fileName, contentType, ct).ConfigureAwait(false);
    }

    // ==========================================
    // RELATED LISTS API (v2 /{module}/{id}/{related_list})
    // ==========================================

    /// <inheritdoc />
    public async Task<string> GetRelatedListRecordsRawAsync(
        BiginDataCenter dc,
        string accessToken,
        string moduleApiName,
        string recordId,
        string relatedListApiName,
        BiginRelatedListQuery? query = null,
        CancellationToken ct = default)
    {
        ValidateAuthToken(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleApiName);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordId);
        ArgumentException.ThrowIfNullOrWhiteSpace(relatedListApiName);

        query?.Validate(relatedListApiName);

        var queryString = query?.ToQueryString(relatedListApiName) ?? string.Empty;
        var url = dc.GetRelatedListEndpointUrl(moduleApiName, recordId, relatedListApiName) + queryString;

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddZohoAuthHeader(request, accessToken);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw BiginApiException.FromHttpResponse(response.StatusCode, content, url);
        }

        return content;
    }

    /// <inheritdoc />
    public async Task<BiginRelatedListResponse> GetRelatedListRecordsAsync(
        BiginDataCenter dc,
        string accessToken,
        string moduleApiName,
        string recordId,
        string relatedListApiName,
        BiginRelatedListQuery? query = null,
        CancellationToken ct = default)
    {
        var json = await GetRelatedListRecordsRawAsync(dc, accessToken, moduleApiName, recordId, relatedListApiName, query, ct).ConfigureAwait(false);
        var parsed = JsonSerializer.Deserialize<BiginRelatedListResponse>(json, DefaultJsonOptions);

        if (parsed is null)
        {
            throw new JsonException("Deserialized BiginRelatedListResponse payload is null.");
        }

        return parsed;
    }
}
