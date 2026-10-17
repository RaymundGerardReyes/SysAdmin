using Pipeline.Infrastructure.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Integration.Tests.Security;

[Trait("Category", "Integration")]
public class DpapiCredentialStoreTests
{
    [Fact]
    public async Task RoundTrip_EncryptsAndDecryptsRefreshToken()
    {
        var store = new DpapiCredentialStore();
        await store.SetRefreshTokenAsync("GoogleSheets", "acc_1", "rt_refresh_token_value_xyz");

        var retrieved = await store.GetRefreshTokenAsync("GoogleSheets", "acc_1");
        retrieved.ShouldBe("rt_refresh_token_value_xyz");
    }

    [Fact]
    public async Task Removal_DeletesToken()
    {
        var store = new DpapiCredentialStore();
        await store.SetRefreshTokenAsync("GoogleSheets", "acc_2", "token_to_remove");
        await store.RemoveRefreshTokenAsync("GoogleSheets", "acc_2");

        var retrieved = await store.GetRefreshTokenAsync("GoogleSheets", "acc_2");
        retrieved.ShouldBeNull();
    }
}
