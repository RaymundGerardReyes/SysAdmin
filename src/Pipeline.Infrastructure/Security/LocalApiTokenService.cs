using System.Security.Cryptography;
using System.Text;

namespace Pipeline.Infrastructure.Security;

internal sealed class LocalApiTokenService
{
    private readonly string _launchToken;

    public LocalApiTokenService()
    {
        _launchToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    public LocalApiTokenService(string explicitToken)
    {
        _launchToken = explicitToken;
    }

    public string GetToken() => _launchToken;

    public bool ValidateToken(string? candidateToken)
    {
        if (string.IsNullOrWhiteSpace(candidateToken))
        {
            return false;
        }

        var candidateBytes = Encoding.UTF8.GetBytes(candidateToken.Trim());
        var tokenBytes = Encoding.UTF8.GetBytes(_launchToken);

        if (candidateBytes.Length != tokenBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(candidateBytes, tokenBytes);
    }
}
