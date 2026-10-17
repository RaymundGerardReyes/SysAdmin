namespace Integrations.Google.Gmail;

internal sealed class GmailConnector
{
    private readonly HttpClient _httpClient;

    public GmailConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> SendMessageRawAsync(string rawMessage, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync("https://gmail.googleapis.com/gmail/v1/users/me/messages/send", new StringContent(rawMessage), ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }
}
