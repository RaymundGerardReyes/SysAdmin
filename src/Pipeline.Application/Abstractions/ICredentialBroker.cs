using Pipeline.Application.Authorization;

namespace Pipeline.Application.Abstractions;

public interface ICredentialBroker
{
    Task<AccessToken> GetAccessTokenAsync(
        AuthorizationTicket ticket,
        string provider,
        string accountId,
        CancellationToken ct = default);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc)
{
    public override string ToString() => "[REDACTED_ACCESS_TOKEN]";
}
