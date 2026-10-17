namespace Integrations.Zoho.Bigin;

/// <summary>
/// OAuth scopes required by Zoho Bigin Notifications APIs.
/// </summary>
public static class BiginNotificationScopes
{
    public const string All = "ZohoBigin.notifications.ALL";
    public const string Write = "ZohoBigin.notifications.WRITE";
    public const string Create = "ZohoBigin.notifications.CREATE";
    public const string Read = "ZohoBigin.notifications.READ";
    public const string Update = "ZohoBigin.notifications.UPDATE";
    public const string Delete = "ZohoBigin.notifications.DELETE";

    public static readonly IReadOnlySet<string> ValidEnableScopes = new HashSet<string>(StringComparer.Ordinal)
    {
        All,
        Write,
        Create
    };

    public static readonly IReadOnlySet<string> ValidReadScopes = new HashSet<string>(StringComparer.Ordinal)
    {
        All,
        Read
    };

    public static readonly IReadOnlySet<string> ValidUpdateScopes = new HashSet<string>(StringComparer.Ordinal)
    {
        All,
        Write,
        Update
    };

    public static readonly IReadOnlySet<string> ValidDeleteScopes = new HashSet<string>(StringComparer.Ordinal)
    {
        All,
        Delete
    };

    public static bool IsValidEnableScope(string scope) => ValidEnableScopes.Contains(scope);
    public static bool IsValidReadScope(string scope) => ValidReadScopes.Contains(scope);
    public static bool IsValidUpdateScope(string scope) => ValidUpdateScopes.Contains(scope);
    public static bool IsValidDeleteScope(string scope) => ValidDeleteScopes.Contains(scope);
}
