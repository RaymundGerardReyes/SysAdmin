namespace Integrations.Google.Chat;

internal sealed class ChatConnector
{
    private readonly HttpClient _httpClient;

    public ChatConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> CreateMessageRawAsync(string spaceId, string message, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync($"https://chat.googleapis.com/v1/{spaceId}/messages", new StringContent(message), ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }
}
