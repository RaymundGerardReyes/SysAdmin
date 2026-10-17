namespace Integrations.Zoho.Bigin;

/// <summary>
/// Indicates how a Zoho Bigin module is generated:
/// - default: Generated as default (e.g. Pipelines, Contacts, Accounts)
/// - linking: Generated as linking module
/// - subform: Generated as subform
/// - web: Generated as web-tab widget
/// - custom: Generated as custom module
/// </summary>
public static class BiginGeneratedTypes
{
    public const string Default = "default";
    public const string Linking = "linking";
    public const string Subform = "subform";
    public const string Web = "web";
    public const string Custom = "custom";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Default,
        Linking,
        Subform,
        Web,
        Custom
    };

    public static bool IsValid(string? generatedType) =>
        generatedType is not null && All.Contains(generatedType);
}
