namespace Integrations.Zoho.Bigin;

/// <summary>
/// Supported Bigin module API names for notifications according to Zoho Bigin documentation.
/// </summary>
public static class BiginModules
{
    public const string Contacts = "Contacts";
    public const string Pipelines = "Pipelines";
    public const string Accounts = "Accounts";
    public const string Products = "Products";
    public const string Tasks = "Tasks";
    public const string Events = "Events";
    public const string Calls = "Calls";
    public const string AssociatedProducts = "Associated_Products";
    public const string Notes = "Notes";
    public const string Attachments = "Attachments";
    public const string Social = "Social";

    public const string Companies = "Companies"; // Documentation alias for Accounts

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Contacts,
        Pipelines,
        Accounts,
        Products,
        Tasks,
        Events,
        Calls,
        AssociatedProducts,
        Notes,
        Attachments,
        Social
    };

    /// <summary>
    /// Normalizes a Bigin module name to its official API name.
    /// Specifically maps the documented alias 'Companies' to 'Accounts'.
    /// </summary>
    public static string NormalizeModuleApiName(string? module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            return string.Empty;
        }

        var trimmed = module.Trim();
        if (string.Equals(trimmed, Companies, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmed, Accounts, StringComparison.OrdinalIgnoreCase))
        {
            return Accounts;
        }

        if (string.Equals(trimmed, Contacts, StringComparison.OrdinalIgnoreCase))
        {
            return Contacts;
        }

        if (string.Equals(trimmed, Pipelines, StringComparison.OrdinalIgnoreCase))
        {
            return Pipelines;
        }

        if (string.Equals(trimmed, Products, StringComparison.OrdinalIgnoreCase))
        {
            return Products;
        }

        return trimmed;
    }

    public static bool IsKnownModule(string? module)
    {
        if (string.IsNullOrWhiteSpace(module))
        {
            return false;
        }

        var normalized = NormalizeModuleApiName(module);
        return All.Contains(normalized);
    }
}
