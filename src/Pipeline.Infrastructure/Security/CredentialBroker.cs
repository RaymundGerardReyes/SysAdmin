using System.Collections.Concurrent;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Contracts.Errors;

namespace Pipeline.Infrastructure.Security;

internal sealed class CredentialBroker : ICredentialBroker
{
    private readonly ICredentialStore _credentialStore;
    private readonly IClock _clock;
    private readonly IAuditWriter _auditWriter;
    private readonly ConcurrentDictionary<string, byte> _usedNonces = new();

    public CredentialBroker(
        ICredentialStore credentialStore,
        IClock clock,
        IAuditWriter auditWriter)
    {
        _credentialStore = credentialStore;
        _clock = clock;
        _auditWriter = auditWriter;
    }

    public async Task<AccessToken> GetAccessTokenAsync(
        AuthorizationTicket ticket,
        string provider,
        string accountId,
        CancellationToken ct = default)
    {
        var now = _clock.UtcNow;

        if (ticket is null)
        {
            await AuditDeniedAsync("NULL_TICKET", provider, ct).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Credential access denied: Null authorization ticket.");
        }

        // 1. Verify MAC
        if (!ticket.VerifyMac())
        {
            await AuditDeniedAsync("INVALID_MAC", provider, ct).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Credential access denied: Invalid ticket signature.");
        }

        // 2. Verify Expiry
        if (ticket.ExpiresAtUtc <= now)
        {
            await AuditDeniedAsync("TICKET_EXPIRED", provider, ct).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Credential access denied: Ticket has expired.");
        }

        // 3. Verify Single-use Nonce (atomic mark-used)
        if (!_usedNonces.TryAdd(ticket.Nonce, 0))
        {
            await AuditDeniedAsync("NONCE_REPLAYED", provider, ct).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Credential access denied: Ticket nonce has already been used.");
        }

        // 4. Verify Provider match
        if (ticket.Provider is not null && !string.Equals(ticket.Provider, provider, StringComparison.OrdinalIgnoreCase))
        {
            await AuditDeniedAsync("PROVIDER_MISMATCH", provider, ct).ConfigureAwait(false);
            throw new UnauthorizedAccessException($"Credential access denied: Ticket is bound to provider '{ticket.Provider}', not '{provider}'.");
        }

        // 5. Verify Account match
        if (ticket.AccountId is not null && !string.Equals(ticket.AccountId, accountId, StringComparison.OrdinalIgnoreCase))
        {
            await AuditDeniedAsync("ACCOUNT_MISMATCH", provider, ct).ConfigureAwait(false);
            throw new UnauthorizedAccessException($"Credential access denied: Ticket is bound to account '{ticket.AccountId}', not '{accountId}'.");
        }

        // 6. Verify Fingerprint present
        if (string.IsNullOrWhiteSpace(ticket.OperationFingerprint))
        {
            await AuditDeniedAsync("FINGERPRINT_MISSING", provider, ct).ConfigureAwait(false);
            throw new UnauthorizedAccessException("Credential access denied: Ticket missing operation fingerprint.");
        }

        // Release credential
        var refreshToken = await _credentialStore.GetRefreshTokenAsync(provider, accountId, ct).ConfigureAwait(false);
        var tokenValue = $"mock_token_{Guid.NewGuid():N}";
        return new AccessToken(tokenValue, now.AddHours(1));
    }

    private async Task AuditDeniedAsync(string reason, string provider, CancellationToken ct)
    {
        var entry = new AuditEntry(
            "SYSTEM",
            "CredentialBroker.GetAccessToken",
            "Deny",
            $"{ErrorCodes.CredentialDenied}:{reason}",
            null,
            Guid.NewGuid().ToString("N"),
            null,
            _clock.UtcNow);

        await _auditWriter.WriteAsync(entry, ct).ConfigureAwait(false);
    }
}
