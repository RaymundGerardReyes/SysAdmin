using System.Reflection;
using Pipeline.Application.Authorization;
using Pipeline.Infrastructure.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Architecture.Tests;

[Trait("Category", "Architecture")]
public class NoDirectCredentialAccessTests
{
    [Fact]
    public void DpapiCredentialStore_IsInternal()
    {
        typeof(DpapiCredentialStore).IsNotPublic.ShouldBeTrue("DpapiCredentialStore must be internal (C2)");
    }

    [Fact]
    public void AuthorizationTicket_ConstructorIsInternal()
    {
        var ctors = typeof(AuthorizationTicket).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        ctors.ShouldBeEmpty("AuthorizationTicket must not have public constructors; capability tickets can only be minted internally (C3)");
    }
}
