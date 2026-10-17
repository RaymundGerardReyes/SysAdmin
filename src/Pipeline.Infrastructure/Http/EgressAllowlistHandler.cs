using System.Net;

namespace Pipeline.Infrastructure.Http;

internal sealed class EgressAllowlistHandler : DelegatingHandler
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "sheets.googleapis.com",
        "gmail.googleapis.com",
        "chat.googleapis.com",
        "api.paymongo.com",
        "accounts.zoho.com",
        "expense.zoho.com",
        "bigin.zoho.com"
    };

    public EgressAllowlistHandler(HttpMessageHandler? innerHandler = null)
    {
        InnerHandler = innerHandler ?? new HttpClientHandler { AllowAutoRedirect = false };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri;
        if (uri is null)
        {
            throw new HttpRequestException("Request URI is null.");
        }

        // 1. Must be HTTPS
        if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            throw new HttpRequestException($"SSRF guard: Scheme must be HTTPS, got '{uri.Scheme}'.");
        }

        // 2. Reject UserInfo tricks (e.g. https://sheets.googleapis.com@evil.com/)
        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new HttpRequestException("SSRF guard: UserInfo in URI is forbidden.");
        }

        // 3. Reject IP Literals (e.g. 127.0.0.1, 169.254.169.254)
        if (IPAddress.TryParse(uri.Host, out _))
        {
            throw new HttpRequestException($"SSRF guard: IP literal hosts are forbidden ('{uri.Host}').");
        }

        // 4. Host must be strictly in AllowedHosts (rejecting suffix tricks)
        if (!AllowedHosts.Contains(uri.Host))
        {
            throw new HttpRequestException($"SSRF guard: Host '{uri.Host}' is not in the egress allowlist.");
        }

        // 5. Strip any caller-supplied Authorization header
        request.Headers.Remove("Authorization");

        var response = await base.SendAsync(request, ct).ConfigureAwait(false);

        // 6. Inspect redirects
        if (response.StatusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectMethod or HttpStatusCode.RedirectKeepVerb)
        {
            var location = response.Headers.Location;
            if (location is not null)
            {
                var targetHost = location.IsAbsoluteUri ? location.Host : uri.Host;
                if (!AllowedHosts.Contains(targetHost))
                {
                    throw new HttpRequestException($"SSRF guard: Redirect to disallowed host '{targetHost}' blocked.");
                }
            }
        }

        return response;
    }
}
