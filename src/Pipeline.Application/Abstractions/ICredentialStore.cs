namespace Pipeline.Application.Abstractions;

public interface ICredentialStore
{
    Task<string?> GetRefreshTokenAsync(string provider, string accountId, CancellationToken ct = default);
    Task SetRefreshTokenAsync(string provider, string accountId, string refreshToken, CancellationToken ct = default);
    Task RemoveRefreshTokenAsync(string provider, string accountId, CancellationToken ct = default);
}
