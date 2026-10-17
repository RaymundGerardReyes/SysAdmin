using Integrations.Zoho.Bigin.Models;

namespace Integrations.Zoho.Bigin;

/// <summary>
/// Connector interface for Zoho Bigin v2 APIs (Modules and Notifications).
/// </summary>
internal interface IBiginConnector
{
    /// <summary>
    /// Enable notifications to receive alerts when specific actions are performed within Bigin modules (POST /actions/watch).
    /// </summary>
    Task<BiginWatchResponse> EnableNotificationsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginWatchRequestBody request,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieve a list of user-enabled notifications, optionally filtered by channel_id or module (GET /actions/watch).
    /// </summary>
    Task<BiginGetNotificationDetailsResponse> GetNotificationDetailsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginNotificationDetailsQuery? query = null,
        CancellationToken ct = default);

    /// <summary>
    /// Update user-enabled notification details, overwriting existing channel details (PUT /actions/watch).
    /// </summary>
    Task<BiginWatchResponse> UpdateNotificationDetailsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginWatchRequestBody request,
        CancellationToken ct = default);

    /// <summary>
    /// Update specific information of a notification while retaining existing details (PATCH /actions/watch).
    /// </summary>
    Task<BiginWatchResponse> UpdateSpecificNotificationDetailsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginWatchRequestBody request,
        CancellationToken ct = default);

    /// <summary>
    /// Disable notifications enabled through channels by removing those channels (DELETE /actions/watch?channel_ids=...).
    /// </summary>
    Task<BiginWatchResponse> DisableNotificationsAsync(
        BiginDataCenter dc,
        string accessToken,
        IEnumerable<string> channelIds,
        CancellationToken ct = default);

    /// <summary>
    /// Disable notifications for specific events in a channel (PATCH /actions/watch with _delete_events: true).
    /// </summary>
    Task<BiginWatchResponse> DisableSpecificNotificationsAsync(
        BiginDataCenter dc,
        string accessToken,
        BiginWatchRequestBody request,
        CancellationToken ct = default);

    /// <summary>
    /// Legacy raw get contacts method.
    /// </summary>
    Task<string> GetContactsRawAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the raw JSON string of modules available in the organization from GET /bigin/v2/settings/modules.
    /// </summary>
    Task<string> GetModulesRawAsync(
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves strongly-typed modules metadata from GET /bigin/v2/settings/modules.
    /// </summary>
    Task<BiginModulesResponse> GetModulesAsync(
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default);

    /// <summary>
    /// Finds a specific module by its API name (e.g. "Accounts", "Contacts", "Pipelines").
    /// </summary>
    Task<BiginModule?> GetModuleByApiNameAsync(
        string apiName,
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default);

    /// <summary>
    /// Finds a specific module by its unique ID.
    /// </summary>
    Task<BiginModule?> GetModuleByIdAsync(
        string id,
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all default system modules.
    /// </summary>
    Task<IReadOnlyList<BiginModule>> GetDefaultModulesAsync(
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all modules that support record creation via API.
    /// </summary>
    Task<IReadOnlyList<BiginModule>> GetCreatableModulesAsync(
        BiginDataCenter dataCenter = BiginDataCenter.US,
        string? accessToken = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the raw JSON string of organization details from GET /bigin/v2/org.
    /// </summary>
    Task<string> GetOrgDetailsRawAsync(
        BiginDataCenter dc,
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves strongly-typed organization details from GET /bigin/v2/org.
    /// </summary>
    Task<BiginOrgResponse> GetOrgDetailsAsync(
        BiginDataCenter dc,
        string accessToken,
        CancellationToken ct = default);

    /// <summary>
    /// Uploads an organization photo/logo from a stream via multipart/form-data to POST /bigin/v2/org/photo.
    /// Maximum allowed file size is 1 MB (1,048,576 bytes).
    /// </summary>
    Task<BiginPhotoUploadResponse> UploadOrgPhotoAsync(
        BiginDataCenter dc,
        string accessToken,
        Stream fileStream,
        string fileName,
        string contentType = "image/png",
        CancellationToken ct = default);

    /// <summary>
    /// Uploads an organization photo/logo from a byte array via multipart/form-data to POST /bigin/v2/org/photo.
    /// Maximum allowed file size is 1 MB (1,048,576 bytes).
    /// </summary>
    Task<BiginPhotoUploadResponse> UploadOrgPhotoAsync(
        BiginDataCenter dc,
        string accessToken,
        byte[] fileBytes,
        string fileName,
        string contentType = "image/png",
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the raw JSON string of related list records from GET /bigin/v2/{module_api_name}/{record_id}/{related_list_api_name}.
    /// </summary>
    Task<string> GetRelatedListRecordsRawAsync(
        BiginDataCenter dc,
        string accessToken,
        string moduleApiName,
        string recordId,
        string relatedListApiName,
        BiginRelatedListQuery? query = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves strongly-typed related list records from GET /bigin/v2/{module_api_name}/{record_id}/{related_list_api_name}.
    /// </summary>
    Task<BiginRelatedListResponse> GetRelatedListRecordsAsync(
        BiginDataCenter dc,
        string accessToken,
        string moduleApiName,
        string recordId,
        string relatedListApiName,
        BiginRelatedListQuery? query = null,
        CancellationToken ct = default);
}
