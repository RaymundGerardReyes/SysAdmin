using System.Text.Json;
using System.Text.Json.Serialization;

namespace Integrations.Zoho.Bigin.Models;

/// <summary>
/// Custom converter for DateTimeOffset? that safely handles null, empty string "", whitespace, and ISO8601 strings.
/// </summary>
public sealed class BiginNullableDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(str, out var dto))
            {
                return dto;
            }

            return null;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("O"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// Root response object for GET /bigin/v2/settings/modules.
/// </summary>
public sealed record BiginModulesResponse(
    [property: JsonPropertyName("modules")] IReadOnlyList<BiginModule> Modules
);

/// <summary>
/// Represents a Zoho Bigin module metadata record.
/// </summary>
public sealed record BiginModule(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("module_name")] string ModuleName,
    [property: JsonPropertyName("api_name")] string ApiName,
    [property: JsonPropertyName("singular_label")] string SingularLabel,
    [property: JsonPropertyName("plural_label")] string PluralLabel,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("visible")] bool Visible,
    [property: JsonPropertyName("visibility")] int Visibility,
    [property: JsonPropertyName("viewable")] bool Viewable,
    [property: JsonPropertyName("creatable")] bool Creatable,
    [property: JsonPropertyName("editable")] bool Editable,
    [property: JsonPropertyName("deletable")] bool Deletable,
    [property: JsonPropertyName("convertable")] bool Convertable,
    [property: JsonPropertyName("quick_create")] bool QuickCreate,
    [property: JsonPropertyName("api_supported")] bool ApiSupported,
    [property: JsonPropertyName("global_search_supported")] bool GlobalSearchSupported,
    [property: JsonPropertyName("email_parser_supported")] bool EmailParserSupported,
    [property: JsonPropertyName("inventory_template_supported")] bool InventoryTemplateSupported,
    [property: JsonPropertyName("isBlueprintSupported")] bool IsBlueprintSupported,
    [property: JsonPropertyName("emailTemplate_support")] bool EmailTemplateSupport,
    [property: JsonPropertyName("filter_supported")] bool FilterSupported,
    [property: JsonPropertyName("show_as_tab")] bool ShowAsTab,
    [property: JsonPropertyName("web_link")] string? WebLink,
    [property: JsonPropertyName("sequence_number")] int SequenceNumber,
    [property: JsonPropertyName("generated_type")] string GeneratedType,
    [property: JsonPropertyName("feeds_required")] bool FeedsRequired,
    [property: JsonPropertyName("scoring_supported")] bool ScoringSupported,
    [property: JsonPropertyName("webform_supported")] bool WebformSupported,
    [property: JsonPropertyName("arguments")] IReadOnlyList<BiginArgument>? Arguments,
    [property: JsonConverter(typeof(BiginNullableDateTimeOffsetConverter))]
    [property: JsonPropertyName("modified_time")] DateTimeOffset? ModifiedTime,
    [property: JsonPropertyName("modified_by")] BiginUserReference? ModifiedBy,
    [property: JsonPropertyName("profiles")] IReadOnlyList<BiginProfileReference>? Profiles,
    [property: JsonPropertyName("profile_count")] int ProfileCount,
    [property: JsonPropertyName("business_card_field_limit")] int BusinessCardFieldLimit,
    [property: JsonPropertyName("parent_module")] BiginParentModule? ParentModule,
    [property: JsonPropertyName("presence_sub_menu")] bool PresenceSubMenu,
    [property: JsonPropertyName("triggers_supported")] bool TriggersSupported,
    [property: JsonPropertyName("has_more_profiles")] bool HasMoreProfiles
);

/// <summary>
/// Profile associated with a Bigin module.
/// </summary>
public sealed record BiginProfileReference(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("id")] string Id
);

/// <summary>
/// User who modified the module properties.
/// </summary>
public sealed record BiginUserReference(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("id")] string Id
);

/// <summary>
/// Parent module details.
/// </summary>
public sealed record BiginParentModule(
    [property: JsonPropertyName("api_name")] string? ApiName,
    [property: JsonPropertyName("id")] string? Id
);

/// <summary>
/// Link argument used in a web tab.
/// </summary>
public sealed record BiginArgument(
    [property: JsonPropertyName("display_name")] string? DisplayName,
    [property: JsonPropertyName("value")] string? Value
);

/// <summary>
/// Standard Bigin error payload.
/// </summary>
public sealed record BiginErrorResponse(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("message")] string? Message,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("details")] IReadOnlyDictionary<string, object>? Details
);
