using System.Text.Json;
using System.Text.Json.Serialization;

namespace Integrations.Zoho.Bigin.Models;

/// <summary>
/// Root response object for GET /bigin/v2/org.
/// </summary>
public sealed record BiginOrgResponse(
    [property: JsonPropertyName("org")] IReadOnlyList<BiginOrgDetails> Org
);

/// <summary>
/// Detailed organization profile metadata in Bigin.
/// </summary>
public sealed record BiginOrgDetails(
    [property: JsonPropertyName("country")] string? Country,
    [property: JsonPropertyName("city")] string? City,
    [property: JsonPropertyName("street")] string? Street,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("zip")] string? Zip,
    [property: JsonPropertyName("hierarchy_preferences")] BiginHierarchyPreferences? HierarchyPreferences,
    [property: JsonPropertyName("photo_id")] string? PhotoId,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("mc_status")] bool? McStatus,
    [property: JsonPropertyName("gapps_enabled")] bool? GappsEnabled,
    [property: JsonPropertyName("lite_users_enabled")] bool? LiteUsersEnabled,
    [property: JsonPropertyName("domain_name")] string? DomainName,
    [property: JsonPropertyName("translation_enabled")] bool? TranslationEnabled,
    [property: JsonPropertyName("alias")] string? Alias,
    [property: JsonPropertyName("currency")] string? Currency,
    [property: JsonPropertyName("deletable_org_account")] bool? DeletableOrgAccount,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("fax")] string? Fax,
    [property: JsonPropertyName("mobile")] string? Mobile,
    [property: JsonPropertyName("phone")] string? Phone,
    [property: JsonPropertyName("primary_email")] string? PrimaryEmail,
    [property: JsonPropertyName("employee_count")] int? EmployeeCount,
    [property: JsonPropertyName("website")] string? Website,
    [property: JsonPropertyName("currency_symbol")] string? CurrencySymbol,
    [property: JsonPropertyName("currency_locale")] string? CurrencyLocale,
    [property: JsonPropertyName("primary_zuid")] string? PrimaryZuid,
    [property: JsonPropertyName("zia_portal_id")] string? ZiaPortalId,
    [property: JsonPropertyName("time_zone")] string? TimeZone,
    [property: JsonPropertyName("zgid")] string? Zgid,
    [property: JsonPropertyName("country_code")] string? CountryCode,
    [property: JsonPropertyName("license_details")] BiginLicenseDetails? LicenseDetails,
    [property: JsonPropertyName("company_name")] string? CompanyName,
    [property: JsonPropertyName("privacy_settings")] bool? PrivacySettings,
    [property: JsonPropertyName("hipaa_compliance_enabled")] bool? HipaaComplianceEnabled,
    [property: JsonPropertyName("iso_code")] string? IsoCode
)
{
    public bool IsMcStatus => McStatus ?? false;
    public bool IsGappsEnabled => GappsEnabled ?? false;
    public bool IsLiteUsersEnabled => LiteUsersEnabled ?? false;
    public bool IsTranslationEnabled => TranslationEnabled ?? false;
    public bool IsDeletableOrgAccount => DeletableOrgAccount ?? false;
    public bool IsPrivacySettings => PrivacySettings ?? false;
    public bool IsHipaaComplianceEnabled => HipaaComplianceEnabled ?? false;
}

/// <summary>
/// Hierarchy structure preferences for the organization.
/// </summary>
public sealed record BiginHierarchyPreferences(
    [property: JsonPropertyName("type")] string? Type
);

/// <summary>
/// License information for the Bigin organization.
/// </summary>
public sealed record BiginLicenseDetails(
    [property: JsonPropertyName("paid_expiry")] string? PaidExpiry,
    [property: JsonPropertyName("users_license_purchased")] int? UsersLicensePurchased,
    [property: JsonPropertyName("trial_type")] string? TrialType,
    [property: JsonPropertyName("trial_expiry")] string? TrialExpiry,
    [property: JsonPropertyName("paid")] bool? Paid,
    [property: JsonPropertyName("paid_type")] string? PaidType
)
{
    public bool IsPaid => Paid ?? false;
}

/// <summary>
/// Response object for POST /bigin/v2/org/photo.
/// </summary>
public sealed record BiginPhotoUploadResponse(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("details")] JsonElement? Details,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("status")] string? Status
)
{
    public bool IsSuccess =>
        string.Equals(Status, "success", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Code, "SUCCESS", StringComparison.OrdinalIgnoreCase);
}
