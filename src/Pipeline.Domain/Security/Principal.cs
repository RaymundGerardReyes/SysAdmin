namespace Pipeline.Domain.Security;

public sealed class Principal
{
    public string UserId { get; init; } = default!;
    public IReadOnlySet<string> RoleNames { get; init; } = default!;
    public ResourceScope Scope { get; init; } = ResourceScope.Empty;
    public DateTimeOffset SessionExpiresAtUtc { get; init; }
    public DateTimeOffset? LastStepUpAtUtc { get; init; }

    public Principal(
        string userId,
        IReadOnlySet<string> roleNames,
        ResourceScope? scope = null,
        DateTimeOffset? sessionExpiresAtUtc = null,
        DateTimeOffset? lastStepUpAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        UserId = userId;
        RoleNames = roleNames ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Scope = scope ?? ResourceScope.Empty;
        SessionExpiresAtUtc = sessionExpiresAtUtc ?? DateTimeOffset.UtcNow.AddHours(8);
        LastStepUpAtUtc = lastStepUpAtUtc;
    }

    public bool Has(Permission permission)
    {
        foreach (var roleName in RoleNames)
        {
            if (RoleCatalog.TryGetRole(roleName, out var role) && role is not null)
            {
                if (role.Permissions.Contains(permission))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
