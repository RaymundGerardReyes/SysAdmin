namespace Integrations.PayMongo;

internal sealed class PayMongoConnector
{
    private readonly HttpClient _httpClient;

    public PayMongoConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> CreateCheckoutRawAsync(string payload, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync("https://api.paymongo.com/v1/checkout_sessions", new StringContent(payload), ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }
}
