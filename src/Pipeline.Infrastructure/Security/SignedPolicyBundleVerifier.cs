using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Pipeline.Application.Abstractions;
using Pipeline.Domain.Security;

namespace Pipeline.Infrastructure.Security;

public sealed record SignedPolicyPayload(
    long Version,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyDictionary<string, IReadOnlySet<string>> UserRoles,
    IReadOnlyDictionary<string, ResourceScope> UserScopes,
    string Signature);

internal sealed class SignedPolicyBundleVerifier
{
    private long _lastSeenVersion;
    private readonly byte[] _pinnedKey;

    public SignedPolicyBundleVerifier(byte[]? pinnedKey = null, long initialVersion = 0)
    {
        _pinnedKey = pinnedKey ?? Encoding.UTF8.GetBytes("pipeline_pinned_policy_key_2026_test");
        _lastSeenVersion = initialVersion;
    }

    public bool Verify(SignedPolicyPayload payload, DateTimeOffset nowUtc, out PolicyBundle? bundle, out string? reason)
    {
        bundle = null;

        // 1. Check expiration
        if (payload.ExpiresAtUtc <= nowUtc)
        {
            reason = "POLICY_EXPIRED";
            return false;
        }

        // 2. Anti-rollback check: Version must be >= LastSeenVersion
        if (payload.Version < _lastSeenVersion)
        {
            reason = "POLICY_ROLLBACK_DETECTED";
            return false;
        }

        // 3. Verify signature
        var canonical = $"{payload.Version}|{payload.ExpiresAtUtc.ToUnixTimeSeconds()}|{JsonSerializer.Serialize(payload.UserRoles)}";
        using var hmac = new HMACSHA256(_pinnedKey);
        var expectedSig = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(payload.Signature), Encoding.UTF8.GetBytes(expectedSig)))
        {
            reason = "INVALID_SIGNATURE";
            return false;
        }

        // 4. Validate all roles exist and permissions exist
        foreach (var (user, roles) in payload.UserRoles)
        {
            foreach (var roleName in roles)
            {
                if (!RoleCatalog.TryGetRole(roleName, out var role) || role is null)
                {
                    reason = $"UNKNOWN_ROLE:{roleName}";
                    return false;
                }
            }

            // 5. SoD verification
            if (!SeparationOfDutiesRule.IsRoleSetValid(roles))
            {
                reason = $"SOD_VIOLATION:{user}";
                return false;
            }
        }

        _lastSeenVersion = payload.Version;
        bundle = new PolicyBundle(payload.Version, payload.ExpiresAtUtc, payload.UserRoles, payload.UserScopes);
        reason = null;
        return true;
    }

    public static string CreateSignature(long version, DateTimeOffset expiresAtUtc, IReadOnlyDictionary<string, IReadOnlySet<string>> userRoles, byte[] key)
    {
        var canonical = $"{version}|{expiresAtUtc.ToUnixTimeSeconds()}|{JsonSerializer.Serialize(userRoles)}";
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));
    }
}
