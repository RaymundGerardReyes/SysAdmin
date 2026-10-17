using System.Reflection;
using Pipeline.Application.Authorization;
using Pipeline.Domain.Security;

namespace Pipeline.Application.Pipeline;

public static class UseCaseRegistryValidator
{
    public static void ValidateOrThrow(IEnumerable<Type> handlerTypes)
    {
        ArgumentNullException.ThrowIfNull(handlerTypes);

        // 1. Verify RoleCatalog baseline integrity
        foreach (var (roleName, role) in RoleCatalog.Baseline)
        {
            foreach (var permission in role.Permissions)
            {
                if (!PermissionCatalog.Exists(permission))
                {
                    throw new InvalidOperationException(
                        $"Role '{roleName}' references unknown permission '{permission.Value}'.");
                }
            }
        }

        // 2. Verify SeparationOfDuties on all baseline roles
        foreach (var (roleName, role) in RoleCatalog.Baseline)
        {
            if (!SeparationOfDutiesRule.IsRoleSetValid(new HashSet<string> { roleName }))
            {
                throw new InvalidOperationException(
                    $"Role '{roleName}' violates Separation of Duties invariants.");
            }
        }

        // 3. Verify all use-case handlers have valid [RequiresPermission]
        foreach (var handlerType in handlerTypes)
        {
            if (handlerType.IsAbstract || handlerType.IsInterface)
            {
                continue;
            }

            var attr = handlerType.GetCustomAttribute<RequiresPermissionAttribute>();
            if (attr is null)
            {
                throw new InvalidOperationException(
                    $"Use case handler '{handlerType.FullName}' is missing mandatory [RequiresPermission] attribute.");
            }

            Permission permission;
            try
            {
                permission = new Permission(attr.Permission);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Use case handler '{handlerType.FullName}' specifies invalid permission string '{attr.Permission}'.", ex);
            }

            if (!PermissionCatalog.Exists(permission))
            {
                throw new InvalidOperationException(
                    $"Use case handler '{handlerType.FullName}' specifies unknown permission '{permission.Value}' not present in PermissionCatalog.");
            }
        }
    }
}
