namespace Integrations.Zoho.Bigin;

internal sealed class BiginConnector
{
    private readonly HttpClient _httpClient;

    public BiginConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetContactsRawAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("https://bigin.zoho.com/api/v1/Contacts", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }
}
