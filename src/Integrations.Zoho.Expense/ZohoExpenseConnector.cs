namespace Integrations.Zoho.Expense;

internal sealed class ZohoExpenseConnector
{
    private readonly HttpClient _httpClient;

    public ZohoExpenseConnector(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetExpensesRawAsync(string orgId, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"https://expense.zoho.com/api/v1/expenses?organization_id={orgId}", ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }
}
