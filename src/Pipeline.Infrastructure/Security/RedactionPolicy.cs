using System.Text.RegularExpressions;

namespace Pipeline.Infrastructure.Security;

public static partial class RedactionPolicy
{
    [GeneratedRegex(@"Bearer\s+[A-Za-z0-9\-_\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex(@"""(access_token|refresh_token|client_secret|password)""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SensitiveJsonFieldsRegex();

    [GeneratedRegex(@"(Authorization|Cookie|X-Api-Key)\s*[:=]\s*[^\r\n,;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SensitiveHeaderRegex();

    public static string Redact(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var result = BearerTokenRegex().Replace(input, "Bearer [REDACTED]");
        result = SensitiveJsonFieldsRegex().Replace(result, @"""$1"": ""[REDACTED]""");
        result = SensitiveHeaderRegex().Replace(result, "$1: [REDACTED]");
        return result;
    }
}
