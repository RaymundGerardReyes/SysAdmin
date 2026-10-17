using Pipeline.Domain.Security;

namespace Pipeline.Application.Abstractions;

public interface IPrincipalAccessor
{
    Principal? Current { get; }
}
