namespace Pipeline.Domain.Security;

public sealed class ResourceScope
{
    public static readonly ResourceScope Empty = new(
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal));

    public IReadOnlySet<string> SpreadsheetIds { get; init; }
    public IReadOnlySet<string> ChatSpaceIds { get; init; }
    public IReadOnlySet<string> ZohoOrganizationIds { get; init; }
    public IReadOnlySet<string> PaymentAccountIds { get; init; }

    public ResourceScope(
        IReadOnlySet<string>? spreadsheetIds = null,
        IReadOnlySet<string>? chatSpaceIds = null,
        IReadOnlySet<string>? zohoOrganizationIds = null,
        IReadOnlySet<string>? paymentAccountIds = null)
    {
        SpreadsheetIds = spreadsheetIds ?? new HashSet<string>(StringComparer.Ordinal);
        ChatSpaceIds = chatSpaceIds ?? new HashSet<string>(StringComparer.Ordinal);
        ZohoOrganizationIds = zohoOrganizationIds ?? new HashSet<string>(StringComparer.Ordinal);
        PaymentAccountIds = paymentAccountIds ?? new HashSet<string>(StringComparer.Ordinal);
    }
}
