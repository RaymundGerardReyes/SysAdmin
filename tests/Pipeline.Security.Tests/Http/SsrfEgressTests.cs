using System.Net;
using Pipeline.Infrastructure.Http;
using Shouldly;
using Xunit;

namespace Pipeline.Security.Tests.Http;

[Trait("Category", "Security")]
public class SsrfEgressTests
{
    private sealed class StubInnerHandler : HttpMessageHandler
    {
        public bool Invoked { get; private set; }
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Invoked = true;
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Theory]
    [InlineData("http://sheets.googleapis.com/")]            // Not HTTPS
    [InlineData("https://evil.example.com/")]                // Disallowed host
    [InlineData("https://127.0.0.1/")]                       // Loopback IP literal
    [InlineData("https://169.254.169.254/")]                 // Cloud metadata IP
    [InlineData("https://sheets.googleapis.com@evil.com/")]  // UserInfo trick
    [InlineData("https://sheets.googleapis.com.evil.com/")]  // Suffix trick
    public async Task Egress_Blocks(string url)
    {
        var inner = new StubInnerHandler();
        var handler = new EgressAllowlistHandler(inner);
        var client = new HttpClient(handler);

        await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await client.GetAsync(url);
        });

        inner.Invoked.ShouldBeFalse("Disallowed destination must be blocked before invoking inner transport");
    }

    [Fact]
    public async Task CallerSuppliedAuthorizationHeader_IsStripped()
    {
        var inner = new StubInnerHandler();
        var handler = new EgressAllowlistHandler(inner);
        var client = new HttpClient(handler);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://sheets.googleapis.com/v4/test");
        request.Headers.Add("Authorization", "Bearer attacker_supplied_token");

        await client.SendAsync(request);

        inner.Invoked.ShouldBeTrue();
        inner.CapturedRequest!.Headers.Contains("Authorization").ShouldBeFalse("Caller-supplied Authorization header must be stripped");
    }

    [Theory]
    [InlineData("https://www.zohoapis.com/bigin/v2/actions/watch")]
    [InlineData("https://zohoapis.com/bigin/v2/actions/watch")]
    [InlineData("https://www.zohoapis.eu/bigin/v2/actions/watch")]
    [InlineData("https://zohoapis.eu/bigin/v2/actions/watch")]
    [InlineData("https://www.zohoapis.com.au/bigin/v2/actions/watch")]
    [InlineData("https://zohoapis.com.au/bigin/v2/actions/watch")]
    [InlineData("https://www.zohoapis.in/bigin/v2/actions/watch")]
    [InlineData("https://zohoapis.in/bigin/v2/actions/watch")]
    [InlineData("https://www.zohoapis.com.cn/bigin/v2/actions/watch")]
    [InlineData("https://zohoapis.com.cn/bigin/v2/actions/watch")]
    [InlineData("https://www.zohoapis.jp/bigin/v2/actions/watch")]
    [InlineData("https://zohoapis.jp/bigin/v2/actions/watch")]
    [InlineData("https://www.zohoapis.sa/bigin/v2/actions/watch")]
    [InlineData("https://zohoapis.sa/bigin/v2/actions/watch")]
    [InlineData("https://www.zohoapis.ca/bigin/v2/actions/watch")]
    [InlineData("https://zohoapis.ca/bigin/v2/actions/watch")]
    public async Task Egress_Allows_AllEightZohoBiginDataCenters(string url)
    {
        var inner = new StubInnerHandler();
        var handler = new EgressAllowlistHandler(inner);
        var client = new HttpClient(handler);

        var response = await client.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        inner.Invoked.ShouldBeTrue("Valid Bigin DC endpoint must be allowed through egress guard");
    }

    [Theory]
    [InlineData("https://evil.zohoapis.com/")]
    [InlineData("https://attacker.zohoapis.eu/")]
    [InlineData("https://www.zohoapis.com.evil.com/")]
    [InlineData("https://www.zohoapis.ca@evil.com/")]
    public async Task Egress_Blocks_DisallowedZohoSubdomainsAndAttacks(string url)
    {
        var inner = new StubInnerHandler();
        var handler = new EgressAllowlistHandler(inner);
        var client = new HttpClient(handler);

        await Should.ThrowAsync<HttpRequestException>(async () =>
        {
            await client.GetAsync(url);
        });

        inner.Invoked.ShouldBeFalse("Disallowed or attacker zoho subdomain must be blocked");
    }
}
