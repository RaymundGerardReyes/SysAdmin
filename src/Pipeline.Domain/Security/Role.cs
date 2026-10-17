namespace Pipeline.Domain.Security;

public sealed record Role(string Name, IReadOnlySet<Permission> Permissions);
