using System.Reflection;
using Pipeline.Application.Authorization;
using Pipeline.Application.UseCases;
using Pipeline.Domain.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Architecture.Tests;

[Trait("Category", "Architecture")]
public class EveryUseCaseHasPermissionTests
{
    [Fact]
    public void EveryHandler_HasRequiresPermission_WithCatalogPermission()
    {
        var appAssembly = typeof(IUseCaseHandler).Assembly;

        var handlerTypes = appAssembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IUseCaseHandler).IsAssignableFrom(t))
            .ToList();

        handlerTypes.ShouldNotBeEmpty();

        foreach (var handler in handlerTypes)
        {
            var attr = handler.GetCustomAttribute<RequiresPermissionAttribute>();
            attr.ShouldNotBeNull($"Handler {handler.FullName} must declare [RequiresPermission] (C4)");

            var permission = new Permission(attr.Permission);
            PermissionCatalog.Exists(permission).ShouldBeTrue($"Permission '{permission.Value}' on {handler.Name} must exist in PermissionCatalog");
        }
    }
}
