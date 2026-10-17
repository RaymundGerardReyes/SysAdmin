using Pipeline.Domain.Security;

namespace Pipeline.Application.Abstractions;

public sealed record PolicyBundle(
    long Version,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyDictionary<string, IReadOnlySet<string>> UserRoles,
    IReadOnlyDictionary<string, ResourceScope> UserScopes);

public interface IPolicyBundleProvider
{
    PolicyBundle? Current { get; }
    Task<bool> RefreshAsync(CancellationToken ct = default);
}
