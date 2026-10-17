using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Errors;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;
using Pipeline.Domain.Security;

namespace Pipeline.Application.UseCases.Admin;

public sealed record ManageUserRolesCommand(
    string TargetUserId,
    IReadOnlySet<string> NewRoleNames) : IUseCaseRequest
{
    public string OperationName => "admin.users.manage";
    public IReadOnlyDictionary<string, string> GetResourceIds() =>
        new Dictionary<string, string>(StringComparer.Ordinal) { ["targetUserId"] = TargetUserId };
}

[RequiresPermission("admin.users.manage")]
public sealed class ManageUserRolesUseCase : IUseCaseHandler<ManageUserRolesCommand, ManageUserRolesResponse>
{
    public Task<Result<ManageUserRolesResponse>> HandleAsync(
        ManageUserRolesCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        // Enforce Separation of Duties: cannot manage own roles
        if (SeparationOfDutiesRule.IsSelfModificationAttempt(context.Principal.UserId, request.TargetUserId))
        {
            return Task.FromResult(Result<ManageUserRolesResponse>.Failure(
                ErrorCodes.SodViolation,
                "A principal cannot modify their own roles.",
                "SEC_RBAC_04"));
        }

        // Validate that new role set complies with SoD
        if (!SeparationOfDutiesRule.IsRoleSetValid(request.NewRoleNames))
        {
            return Task.FromResult(Result<ManageUserRolesResponse>.Failure(
                ErrorCodes.SodViolation,
                "The assigned role set violates Separation of Duties constraints."));
        }

        return Task.FromResult(Result<ManageUserRolesResponse>.Success(
            new ManageUserRolesResponse(request.TargetUserId, request.NewRoleNames, DateTimeOffset.UtcNow)));
    }
}
