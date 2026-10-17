using Pipeline.Application.Abstractions;
using Pipeline.Infrastructure.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Security.Tests.Secrets;

[Trait("Category", "Security")]
public class LogRedactionTests
{
    [Fact]
    public void Logs_NeverContain_BearerTokens()
    {
        var rawLog = "Incoming request with Authorization: Bearer secret_token_123456789 and cookie";
        var redacted = RedactionPolicy.Redact(rawLog);

        redacted.ShouldNotContain("secret_token_123456789");
        redacted.ShouldContain("Authorization: [REDACTED]");
    }

    [Fact]
    public void AccessToken_ToString_IsRedacted()
    {
        var token = new AccessToken("super_secret_raw_jwt_value", DateTimeOffset.UtcNow.AddHours(1));
        var str = token.ToString();

        str.ShouldBe("[REDACTED_ACCESS_TOKEN]");
        str.ShouldNotContain("super_secret_raw_jwt_value");
    }

    [Fact]
    public void SensitiveJsonFields_AreRedacted()
    {
        var json = "{\"access_token\": \"secret_xyz\", \"client_secret\": \"cs_999\", \"user\": \"alice\"}";
        var redacted = RedactionPolicy.Redact(json);

        redacted.ShouldNotContain("secret_xyz");
        redacted.ShouldNotContain("cs_999");
        redacted.ShouldContain("\"user\": \"alice\"");
    }
}
