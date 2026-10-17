using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Pipeline.Application.Abstractions;

namespace Pipeline.Infrastructure.Security;

internal sealed class DpapiCredentialStore : ICredentialStore
{
    private readonly ConcurrentDictionary<string, byte[]> _store = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);

    public async Task<string?> GetRefreshTokenAsync(string provider, string accountId, CancellationToken ct = default)
    {
        var key = $"{provider}:{accountId}";
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (!_store.TryGetValue(key, out var cipherBytes))
            {
                return null;
            }

            if (OperatingSystem.IsWindows())
            {
                var plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                try
                {
                    return Encoding.UTF8.GetString(plainBytes);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plainBytes);
                }
            }
            else
            {
                return Encoding.UTF8.GetString(cipherBytes);
            }
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task SetRefreshTokenAsync(string provider, string accountId, string refreshToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var key = $"{provider}:{accountId}";
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(refreshToken);
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                    _store[key] = cipherBytes;
                }
                else
                {
                    _store[key] = plainBytes;
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plainBytes);
            }
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task RemoveRefreshTokenAsync(string provider, string accountId, CancellationToken ct = default)
    {
        var key = $"{provider}:{accountId}";
        var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _store.TryRemove(key, out _);
        }
        finally
        {
            sem.Release();
        }
    }
}
