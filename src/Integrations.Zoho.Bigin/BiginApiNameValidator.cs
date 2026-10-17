using System.Text.RegularExpressions;

namespace Integrations.Zoho.Bigin;

/// <summary>
/// Validates Zoho Bigin module API names according to the Zoho Bigin Modules API specification:
/// - Must consist of alphabets, numbers, and underscores.
/// - Must start with an alphabet.
/// - Must not have consecutive underscores.
/// - Must not end with an underscore.
/// </summary>
public static partial class BiginApiNameValidator
{
    // Standard system modules
    public const string AccountsApiName = "Accounts";
    public const string ContactsApiName = "Contacts";
    public const string PipelinesApiName = "Pipelines";
    public const string TasksApiName = "Tasks";
    public const string EventsApiName = "Events";
    public const string CallsApiName = "Calls";
    public const string ProductsApiName = "Products";
    public const string AssociatedProductsApiName = "Associated_Products";
    public const string NotesApiName = "Notes";
    public const string AttachmentsApiName = "Attachments";
    public const string SocialApiName = "Social";

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex ApiNamePattern();

    public static bool IsValid(string? apiName)
    {
        if (string.IsNullOrWhiteSpace(apiName))
        {
            return false;
        }

        return ApiNamePattern().IsMatch(apiName);
    }

    /// <summary>
    /// Normalizes common display names or variations to their official Bigin API name.
    /// Per Zoho Bigin documentation: The api_name for the Companies module in Bigin is Accounts.
    /// "Associated Products" maps to "Associated_Products".
    /// </summary>
    public static string NormalizeApiName(string? moduleNameOrApiName)
    {
        if (string.IsNullOrWhiteSpace(moduleNameOrApiName))
        {
            return string.Empty;
        }

        var trimmed = moduleNameOrApiName.Trim();

        if (string.Equals(trimmed, "Companies", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "Company", StringComparison.OrdinalIgnoreCase))
        {
            return AccountsApiName;
        }

        if (string.Equals(trimmed, "Associated Products", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "AssociatedProducts", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, "LinkingModule1", StringComparison.OrdinalIgnoreCase))
        {
            return AssociatedProductsApiName;
        }

        return trimmed;
    }

    public static void ValidateOrThrow(string? apiName)
    {
        if (string.IsNullOrWhiteSpace(apiName))
        {
            throw new ArgumentException("Bigin module api_name cannot be null or empty.", nameof(apiName));
        }

        if (!char.IsAsciiLetter(apiName[0]))
        {
            throw new ArgumentException($"Bigin module api_name '{apiName}' must start with an alphabet.", nameof(apiName));
        }

        if (apiName.Contains("__", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Bigin module api_name '{apiName}' cannot contain consecutive underscores.", nameof(apiName));
        }

        if (apiName.EndsWith('_'))
        {
            throw new ArgumentException($"Bigin module api_name '{apiName}' cannot end with an underscore.", nameof(apiName));
        }

        if (!IsValid(apiName))
        {
            throw new ArgumentException($"Bigin module api_name '{apiName}' is invalid. It must consist of alphabets, numbers, and underscores only.", nameof(apiName));
        }
    }
}
