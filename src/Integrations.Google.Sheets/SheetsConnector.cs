namespace Integrations.Google.Sheets;

internal sealed class SheetsConnector
{
    private readonly HttpClient _httpClient;

    public SheetsConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> ReadRangeRawAsync(string spreadsheetId, string range, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{range}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }
}
