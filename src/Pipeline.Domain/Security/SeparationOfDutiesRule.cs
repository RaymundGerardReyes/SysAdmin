namespace Pipeline.Domain.Security;

public static class SeparationOfDutiesRule
{
    public static bool IsApproverAllowed(string requesterUserId, string approverUserId)
    {
        if (string.IsNullOrWhiteSpace(requesterUserId) || string.IsNullOrWhiteSpace(approverUserId))
        {
            return false;
        }

        return !string.Equals(requesterUserId.Trim(), approverUserId.Trim(), StringComparison.Ordinal);
    }

    public static bool IsRoleSetValid(IReadOnlySet<string> roleNames)
    {
        if (roleNames is null || roleNames.Count == 0)
        {
            return true;
        }

        var aggregatedPermissions = new HashSet<Permission>();
        var isAuditor = false;

        foreach (var roleName in roleNames)
        {
            if (RoleCatalog.TryGetRole(roleName, out var role) && role is not null)
            {
                if (string.Equals(roleName, RoleCatalog.AuditorRole, StringComparison.OrdinalIgnoreCase))
                {
                    isAuditor = true;
                }

                foreach (var permission in role.Permissions)
                {
                    aggregatedPermissions.Add(permission);
                }
            }
        }

        // Rule 1: No single principal may hold both payments.checkout.create and payments.checkout.approve
        if (aggregatedPermissions.Contains(PermissionCatalog.PaymentsCheckoutCreate) &&
            aggregatedPermissions.Contains(PermissionCatalog.PaymentsCheckoutApprove))
        {
            return false;
        }

        // Rule 2: Auditor can never hold any T2 or T3 write permissions
        if (isAuditor)
        {
            foreach (var permission in aggregatedPermissions)
            {
                if (PermissionCatalog.TryGetTier(permission, out var tier))
                {
                    if (tier is RiskTier.T2 or RiskTier.T3 &&
                        !permission.Value.EndsWith(".read", StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public static bool IsSelfModificationAttempt(string actingUserId, string targetUserId)
    {
        if (string.IsNullOrWhiteSpace(actingUserId) || string.IsNullOrWhiteSpace(targetUserId))
        {
            return false;
        }

        return string.Equals(actingUserId.Trim(), targetUserId.Trim(), StringComparison.Ordinal);
    }
}
