using Pipeline.Domain.Security;

namespace Pipeline.TestKit.Builders;

public sealed class PrincipalBuilder
{
    private string _userId = "test_user";
    private readonly HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase) { RoleCatalog.ViewerRole };
    private ResourceScope _scope = ResourceScope.Empty;
    private DateTimeOffset _expiresAtUtc = DateTimeOffset.UtcNow.AddHours(4);
    private DateTimeOffset? _stepUpAtUtc;

    public static PrincipalBuilder For(string userId) => new() { _userId = userId };

    public PrincipalBuilder WithRoles(params string[] roles)
    {
        _roles.Clear();
        foreach (var r in roles) _roles.Add(r);
        return this;
    }

    public PrincipalBuilder WithScope(ResourceScope scope)
    {
        _scope = scope;
        return this;
    }

    public PrincipalBuilder WithSpreadsheet(string spreadsheetId)
    {
        var existing = new HashSet<string>(_scope.SpreadsheetIds, StringComparer.Ordinal) { spreadsheetId };
        _scope = new ResourceScope(existing, _scope.ChatSpaceIds, _scope.ZohoOrganizationIds, _scope.PaymentAccountIds);
        return this;
    }

    public PrincipalBuilder WithPaymentAccount(string accountId)
    {
        var existing = new HashSet<string>(_scope.PaymentAccountIds, StringComparer.Ordinal) { accountId };
        _scope = new ResourceScope(_scope.SpreadsheetIds, _scope.ChatSpaceIds, _scope.ZohoOrganizationIds, existing);
        return this;
    }

    public PrincipalBuilder ExpiresAt(DateTimeOffset expiresAt)
    {
        _expiresAtUtc = expiresAt;
        return this;
    }

    public PrincipalBuilder StepUpAt(DateTimeOffset? stepUpAt)
    {
        _stepUpAtUtc = stepUpAt;
        return this;
    }

    public Principal Build() =>
        new(_userId, _roles, _scope, _expiresAtUtc, _stepUpAtUtc);
}
