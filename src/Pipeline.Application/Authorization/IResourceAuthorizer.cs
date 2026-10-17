using Pipeline.Domain.Security;

namespace Pipeline.Application.Authorization;

public interface IResourceAuthorizer
{
    bool CanHandle(string provider);
    bool IsInScope(Principal principal, IReadOnlyDictionary<string, string> resourceIds);
}

public sealed class ScopedResourceAuthorizer : IResourceAuthorizer
{
    public bool CanHandle(string provider) => true;

    public bool IsInScope(Principal principal, IReadOnlyDictionary<string, string> resourceIds)
    {
        if (resourceIds is null || resourceIds.Count == 0)
        {
            return true;
        }

        // Canonicalization helper: reject control characters or leading/trailing whitespace
        static bool TryCanonicalize(string id, out string clean)
        {
            clean = id;
            if (string.IsNullOrWhiteSpace(id)) return false;
            if (id != id.Trim()) return false;
            foreach (var ch in id)
            {
                if (char.IsControl(ch)) return false;
            }
            return true;
        }

        if (resourceIds.TryGetValue("spreadsheetId", out var sheetId))
        {
            if (!TryCanonicalize(sheetId, out var clean) || !principal.Scope.SpreadsheetIds.Contains(clean))
            {
                return false;
            }
        }

        if (resourceIds.TryGetValue("chatSpaceId", out var spaceId))
        {
            if (!TryCanonicalize(spaceId, out var clean) || !principal.Scope.ChatSpaceIds.Contains(clean))
            {
                return false;
            }
        }

        if (resourceIds.TryGetValue("zohoOrgId", out var orgId))
        {
            if (!TryCanonicalize(orgId, out var clean) || !principal.Scope.ZohoOrganizationIds.Contains(clean))
            {
                return false;
            }
        }

        if (resourceIds.TryGetValue("paymentAccountId", out var paymentAccId))
        {
            if (!TryCanonicalize(paymentAccId, out var clean) || !principal.Scope.PaymentAccountIds.Contains(clean))
            {
                return false;
            }
        }

        return true;
    }
}
