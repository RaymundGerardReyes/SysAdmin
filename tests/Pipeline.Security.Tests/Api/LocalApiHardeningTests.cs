using Pipeline.Infrastructure.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Security.Tests.Api;

[Trait("Category", "Security")]
public class LocalApiHardeningTests
{
    private readonly LocalApiTokenService _tokenService = new("valid_secret_launch_token_12345");

    [Fact]
    public void SEC_API_NoLaunchToken_401()
    {
        _tokenService.ValidateToken(null).ShouldBeFalse();
        _tokenService.ValidateToken("").ShouldBeFalse();
    }

    [Fact]
    public void SEC_API_WrongLaunchToken_401()
    {
        _tokenService.ValidateToken("wrong_token_guess").ShouldBeFalse();
    }

    [Fact]
    public void SEC_API_ValidToken_200()
    {
        _tokenService.ValidateToken("valid_secret_launch_token_12345").ShouldBeTrue();
    }
}
