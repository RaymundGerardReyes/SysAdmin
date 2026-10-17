namespace Integrations.Zoho.Bigin;

/// <summary>
/// OAuth scopes supported by Zoho Bigin Modules API.
/// </summary>
public static class BiginScopes
{
    public const string SettingsAll = "ZohoBigin.settings.ALL";
    public const string SettingsModulesAll = "ZohoBigin.settings.modules.ALL";
    public const string SettingsModulesRead = "ZohoBigin.settings.modules.READ";

    // Organization Scopes
    public const string OrgAll = "ZohoBigin.org.ALL";
    public const string OrgRead = "ZohoBigin.org.READ";
    public const string OrgCreate = "ZohoBigin.org.CREATE";
    public const string OrgWrite = "ZohoBigin.org.WRITE";

    // Modules / Related Lists Scopes
    public const string ModulesAll = "ZohoBigin.modules.ALL";

    public static string GetModuleScopeAll(string moduleName) =>
        $"ZohoBigin.modules.{moduleName}.ALL";

    public static string GetModuleScopeRead(string moduleName) =>
        $"ZohoBigin.modules.{moduleName}.READ";

    public static readonly IReadOnlySet<string> ValidModulesReadScopes = new HashSet<string>(StringComparer.Ordinal)
    {
        SettingsAll,
        SettingsModulesAll,
        SettingsModulesRead
    };

    public static readonly IReadOnlySet<string> ValidOrgReadScopes = new HashSet<string>(StringComparer.Ordinal)
    {
        OrgAll,
        OrgRead
    };

    public static readonly IReadOnlySet<string> ValidOrgWriteScopes = new HashSet<string>(StringComparer.Ordinal)
    {
        OrgAll,
        OrgCreate,
        OrgWrite
    };

    public static bool IsValidModulesReadScope(string scope) =>
        ValidModulesReadScopes.Contains(scope);

    public static bool IsValidOrgReadScope(string scope) =>
        ValidOrgReadScopes.Contains(scope);

    public static bool IsValidOrgWriteScope(string scope) =>
        ValidOrgWriteScopes.Contains(scope);

    public static bool IsValidRelatedListReadScope(string scope, string? moduleName = null)
    {
        if (string.Equals(scope, ModulesAll, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            var normalized = BiginModules.NormalizeModuleApiName(moduleName);
            return string.Equals(scope, GetModuleScopeAll(normalized), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(scope, GetModuleScopeRead(normalized), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(scope, GetModuleScopeAll(moduleName), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(scope, GetModuleScopeRead(moduleName), StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(scope, $"ZohoBigin.modules.{normalized.ToLowerInvariant()}.ALL", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(scope, $"ZohoBigin.modules.{normalized.ToLowerInvariant()}.READ", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
