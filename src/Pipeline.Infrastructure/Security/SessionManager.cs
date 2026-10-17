using Pipeline.Application.Abstractions;
using Pipeline.Domain.Security;

namespace Pipeline.Infrastructure.Security;

internal sealed class SessionManager : IPrincipalAccessor
{
    private Principal? _current;
    private readonly object _lock = new();

    public Principal? Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    public void EstablishSession(
        string userId,
        IReadOnlySet<string> roleNames,
        ResourceScope? scope = null,
        DateTimeOffset? sessionExpiresAtUtc = null)
    {
        lock (_lock)
        {
            _current = new Principal(
                userId,
                roleNames,
                scope ?? ResourceScope.Empty,
                sessionExpiresAtUtc ?? DateTimeOffset.UtcNow.AddHours(8),
                null);
        }
    }

    public void RecordStepUp(DateTimeOffset stepUpTimeUtc)
    {
        lock (_lock)
        {
            if (_current is not null)
            {
                _current = new Principal(
                    _current.UserId,
                    _current.RoleNames,
                    _current.Scope,
                    _current.SessionExpiresAtUtc,
                    stepUpTimeUtc);
            }
        }
    }

    public void RevokeSession()
    {
        lock (_lock)
        {
            _current = null;
        }
    }
}
