using System.Security.Cryptography;
using System.Text;

namespace Pipeline.Application.Authorization;

public static class TicketKeyProvider
{
    private static readonly byte[] SecretKey = RandomNumberGenerator.GetBytes(32);

    internal static byte[] GetKey() => SecretKey;
}

public sealed class AuthorizationTicket
{
    public string Nonce { get; }
    public string OperationName { get; }
    public string OperationFingerprint { get; }
    public string? Provider { get; }
    public string? AccountId { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public string Mac { get; }

    internal AuthorizationTicket(
        string operationName,
        string operationFingerprint,
        string? provider,
        string? accountId,
        DateTimeOffset expiresAtUtc,
        string? nonce = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationFingerprint);

        Nonce = nonce ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        OperationName = operationName;
        OperationFingerprint = operationFingerprint;
        Provider = provider;
        AccountId = accountId;
        ExpiresAtUtc = expiresAtUtc;
        Mac = ComputeMac(Nonce, OperationName, OperationFingerprint, Provider, AccountId, ExpiresAtUtc, TicketKeyProvider.GetKey());
    }

    public bool VerifyMac() =>
        VerifyMac(TicketKeyProvider.GetKey());

    internal bool VerifyMac(byte[] key)
    {
        var expected = ComputeMac(Nonce, OperationName, OperationFingerprint, Provider, AccountId, ExpiresAtUtc, key);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Mac),
            Encoding.UTF8.GetBytes(expected));
    }

    internal static string ComputeMac(
        string nonce,
        string operationName,
        string operationFingerprint,
        string? provider,
        string? accountId,
        DateTimeOffset expiresAtUtc,
        byte[] key)
    {
        var canonical = $"{nonce}|{operationName}|{operationFingerprint}|{provider ?? ""}|{accountId ?? ""}|{expiresAtUtc.ToUnixTimeSeconds()}";
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash);
    }
}
