#!/usr/bin/env bash
# ==============================================================================
# PipelineSolution Modular Scaffolder: Backend Projects & Sources (Steps 1-3 + Skeletons)
# ==============================================================================

set -euo pipefail

write_file() {
  local target="$1"
  local content="$2"
  local force="${3:-false}"

  if [[ -f "$target" ]] && [[ "$force" != "true" ]]; then
    return 0
  fi

  mkdir -p "$(dirname "$target")"
  printf '%s\n' "$content" > "$target"
}

scaffold_backend_projects() {
  local force="${1:-false}"

  # 1. Pipeline.Contracts.csproj
  write_file "src/Pipeline.Contracts/Pipeline.Contracts.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>' "$force"

  # 2. Pipeline.Domain.csproj
  write_file "src/Pipeline.Domain/Pipeline.Domain.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Pipeline.Contracts\Pipeline.Contracts.csproj" />
  </ItemGroup>
</Project>' "$force"

  # 3. Pipeline.Application.csproj
  write_file "src/Pipeline.Application/Pipeline.Application.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\Pipeline.Contracts\Pipeline.Contracts.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Options" />
  </ItemGroup>
</Project>' "$force"

  # 4. Pipeline.Infrastructure.csproj
  write_file "src/Pipeline.Infrastructure/Pipeline.Infrastructure.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\Pipeline.Application\Pipeline.Application.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Http" />
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="System.Security.Cryptography.ProtectedData" />
    <PackageReference Include="Microsoft.Data.Sqlite" />
  </ItemGroup>
</Project>' "$force"

  # 5. Pipeline.Desktop.Host.csproj
  write_file "src/Pipeline.Desktop.Host/Pipeline.Desktop.Host.csproj" '<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\Pipeline.Application\Pipeline.Application.csproj" />
    <ProjectReference Include="..\Pipeline.Infrastructure\Pipeline.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="Microsoft.Extensions.Options" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>
</Project>' "$force"

  # 6. Pipeline.LocalApi.csproj
  write_file "src/Pipeline.LocalApi/Pipeline.LocalApi.csproj" '<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Pipeline.Domain\Pipeline.Domain.csproj" />
    <ProjectReference Include="..\Pipeline.Contracts\Pipeline.Contracts.csproj" />
    <ProjectReference Include="..\Pipeline.Application\Pipeline.Application.csproj" />
    <ProjectReference Include="..\Pipeline.Infrastructure\Pipeline.Infrastructure.csproj" />
  </ItemGroup>
</Project>' "$force"

  # 7-12. Provider Integration Projects
  local integrations=(
    "Integrations.Google.Sheets"
    "Integrations.Google.Gmail"
    "Integrations.Google.Chat"
    "Integrations.Zoho.Expense"
    "Integrations.Zoho.Bigin"
    "Integrations.PayMongo"
  )

  for integ in "${integrations[@]}"; do
    write_file "src/$integ/$integ.csproj" "<Project Sdk=\"Microsoft.NET.Sdk\">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include=\"..\Pipeline.Domain\Pipeline.Domain.csproj\" />
    <ProjectReference Include=\"..\Pipeline.Contracts\Pipeline.Contracts.csproj\" />
  </ItemGroup>
</Project>" "$force"
  done
}

scaffold_domain_security() {
  local force="${1:-false}"

  # Permission.cs
  write_file "src/Pipeline.Domain/Security/Permission.cs" 'using System.Text.RegularExpressions;

namespace Pipeline.Domain.Security;

public readonly partial record struct Permission
{
    [GeneratedRegex(@"^[a-z]+(\.[a-z]+){1,2}$", RegexOptions.Compiled)]
    private static partial Regex FormatRegex();

    public string Value { get; }

    public Permission(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var trimmed = value.Trim();
        if (!FormatRegex().IsMatch(trimmed))
        {
            throw new ArgumentException(
                $"Permission '\''{value}'\'' is invalid. Format must be lowercase matching ^[a-z]+(\\.[a-z]+){{1,2}}$, e.g. '\''jobs.read'\'' or '\''sheets.values.read\''.",
                nameof(value));
        }

        Value = trimmed;
    }

    public override string ToString() => Value;

    public static implicit operator string(Permission permission) => permission.Value;
    public static explicit operator Permission(string value) => new(value);
}' "$force"

  # PermissionCatalog.cs
  write_file "src/Pipeline.Domain/Security/PermissionCatalog.cs" 'namespace Pipeline.Domain.Security;

public static class PermissionCatalog
{
    public static readonly Permission JobsRead = new("jobs.read");
    public static readonly Permission JobsCancel = new("jobs.cancel");
    public static readonly Permission SheetsValuesRead = new("sheets.values.read");
    public static readonly Permission SheetsValuesWrite = new("sheets.values.write");
    public static readonly Permission GmailMessagesSend = new("gmail.messages.send");
    public static readonly Permission ChatMessagesCreate = new("chat.messages.create");
    public static readonly Permission ZohoExpenseRead = new("zoho.expense.read");
    public static readonly Permission ZohoExpenseCreate = new("zoho.expense.create");
    public static readonly Permission BiginContactsRead = new("bigin.contacts.read");
    public static readonly Permission BiginContactsCreate = new("bigin.contacts.create");
    public static readonly Permission PaymentsStatusRead = new("payments.status.read");
    public static readonly Permission PaymentsCheckoutCreate = new("payments.checkout.create");
    public static readonly Permission PaymentsCheckoutApprove = new("payments.checkout.approve");
    public static readonly Permission IntegrationsConnect = new("integrations.connect");
    public static readonly Permission IntegrationsDisconnect = new("integrations.disconnect");
    public static readonly Permission AdminUsersManage = new("admin.users.manage");
    public static readonly Permission AdminPolicyUpdate = new("admin.policy.update");
    public static readonly Permission AdminAuditRead = new("admin.audit.read");

    public static IReadOnlyDictionary<Permission, RiskTier> All { get; } = new Dictionary<Permission, RiskTier>
    {
        [JobsRead] = RiskTier.T0,
        [JobsCancel] = RiskTier.T1,
        [SheetsValuesRead] = RiskTier.T0,
        [SheetsValuesWrite] = RiskTier.T1,
        [GmailMessagesSend] = RiskTier.T2,
        [ChatMessagesCreate] = RiskTier.T2,
        [ZohoExpenseRead] = RiskTier.T0,
        [ZohoExpenseCreate] = RiskTier.T2,
        [BiginContactsRead] = RiskTier.T0,
        [BiginContactsCreate] = RiskTier.T2,
        [PaymentsStatusRead] = RiskTier.T0,
        [PaymentsCheckoutCreate] = RiskTier.T3,
        [PaymentsCheckoutApprove] = RiskTier.T3,
        [IntegrationsConnect] = RiskTier.T3,
        [IntegrationsDisconnect] = RiskTier.T3,
        [AdminUsersManage] = RiskTier.T3,
        [AdminPolicyUpdate] = RiskTier.T3,
        [AdminAuditRead] = RiskTier.T1
    };

    public static bool Exists(Permission permission) => All.ContainsKey(permission);

    public static bool TryGetTier(Permission permission, out RiskTier tier) =>
        All.TryGetValue(permission, out tier);
}' "$force"

  # RiskTier.cs
  write_file "src/Pipeline.Domain/Security/RiskTier.cs" 'namespace Pipeline.Domain.Security;

public enum RiskTier
{
    T0,
    T1,
    T2,
    T3
}' "$force"

  # Role.cs
  write_file "src/Pipeline.Domain/Security/Role.cs" 'namespace Pipeline.Domain.Security;

public sealed record Role(string Name, IReadOnlySet<Permission> Permissions);' "$force"

  # RoleCatalog.cs
  write_file "src/Pipeline.Domain/Security/RoleCatalog.cs" 'namespace Pipeline.Domain.Security;

public static class RoleCatalog
{
    public const string ViewerRole = "Viewer";
    public const string OperatorRole = "Operator";
    public const string FinanceOperatorRole = "FinanceOperator";
    public const string ApproverRole = "Approver";
    public const string IntegrationAdminRole = "IntegrationAdmin";
    public const string AuditorRole = "Auditor";
    public const string OwnerRole = "Owner";

    public static readonly Role Viewer = new(
        ViewerRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.BiginContactsRead
        });

    public static readonly Role Operator = new(
        OperatorRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.JobsCancel,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.SheetsValuesWrite,
            PermissionCatalog.GmailMessagesSend,
            PermissionCatalog.ChatMessagesCreate,
            PermissionCatalog.BiginContactsRead,
            PermissionCatalog.BiginContactsCreate
        });

    public static readonly Role FinanceOperator = new(
        FinanceOperatorRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.JobsCancel,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.SheetsValuesWrite,
            PermissionCatalog.ZohoExpenseRead,
            PermissionCatalog.ZohoExpenseCreate,
            PermissionCatalog.PaymentsStatusRead,
            PermissionCatalog.PaymentsCheckoutCreate
        });

    public static readonly Role Approver = new(
        ApproverRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.JobsCancel,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.ZohoExpenseRead,
            PermissionCatalog.PaymentsStatusRead,
            PermissionCatalog.PaymentsCheckoutApprove
        });

    public static readonly Role IntegrationAdmin = new(
        IntegrationAdminRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.IntegrationsConnect,
            PermissionCatalog.IntegrationsDisconnect
        });

    public static readonly Role Auditor = new(
        AuditorRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.AdminAuditRead
        });

    public static readonly Role Owner = new(
        OwnerRole,
        new HashSet<Permission>
        {
            PermissionCatalog.JobsRead,
            PermissionCatalog.JobsCancel,
            PermissionCatalog.SheetsValuesRead,
            PermissionCatalog.SheetsValuesWrite,
            PermissionCatalog.GmailMessagesSend,
            PermissionCatalog.ChatMessagesCreate,
            PermissionCatalog.ZohoExpenseRead,
            PermissionCatalog.ZohoExpenseCreate,
            PermissionCatalog.BiginContactsRead,
            PermissionCatalog.BiginContactsCreate,
            PermissionCatalog.PaymentsStatusRead,
            PermissionCatalog.IntegrationsConnect,
            PermissionCatalog.IntegrationsDisconnect,
            PermissionCatalog.AdminUsersManage,
            PermissionCatalog.AdminPolicyUpdate,
            PermissionCatalog.AdminAuditRead
        });

    public static IReadOnlyDictionary<string, Role> Baseline { get; } =
        new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase)
        {
            [ViewerRole] = Viewer,
            [OperatorRole] = Operator,
            [FinanceOperatorRole] = FinanceOperator,
            [ApproverRole] = Approver,
            [IntegrationAdminRole] = IntegrationAdmin,
            [AuditorRole] = Auditor,
            [OwnerRole] = Owner
        };

    public static bool TryGetRole(string roleName, out Role? role) =>
        Baseline.TryGetValue(roleName, out role);
}' "$force"

  # Principal.cs
  write_file "src/Pipeline.Domain/Security/Principal.cs" 'namespace Pipeline.Domain.Security;

public sealed class Principal
{
    public string UserId { get; }
    public IReadOnlySet<string> Roles { get; }
    public IReadOnlySet<Permission> Permissions { get; }
    public ResourceScope Scopes { get; }
    public DateTimeOffset SessionExpiresAtUtc { get; }
    public DateTimeOffset? LastStepUpAtUtc { get; }

    public Principal(
        string userId,
        IReadOnlySet<string> roles,
        IReadOnlySet<Permission> permissions,
        ResourceScope scopes,
        DateTimeOffset sessionExpiresAtUtc,
        DateTimeOffset? lastStepUpAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        UserId = userId;
        Roles = roles ?? new HashSet<string>();
        Permissions = permissions ?? new HashSet<Permission>();
        Scopes = scopes ?? ResourceScope.Empty;
        SessionExpiresAtUtc = sessionExpiresAtUtc;
        LastStepUpAtUtc = lastStepUpAtUtc;
    }

    public bool Has(Permission permission) => Permissions.Contains(permission);

    public bool HasAnyRole(params string[] roleNames) =>
        roleNames.Any(r => Roles.Contains(r));
}' "$force"

  # ResourceScope.cs
  write_file "src/Pipeline.Domain/Security/ResourceScope.cs" 'namespace Pipeline.Domain.Security;

public sealed class ResourceScope
{
    public static readonly ResourceScope Empty = new(
        new HashSet<string>(),
        new HashSet<string>(),
        new HashSet<string>(),
        new HashSet<string>());

    public IReadOnlySet<string> SpreadsheetIds { get; }
    public IReadOnlySet<string> ChatSpaceIds { get; }
    public IReadOnlySet<string> ExpenseOrgIds { get; }
    public IReadOnlySet<string> PaymentAccountIds { get; }

    public ResourceScope(
        IEnumerable<string> spreadsheetIds,
        IEnumerable<string> chatSpaceIds,
        IEnumerable<string> expenseOrgIds,
        IEnumerable<string> paymentAccountIds)
    {
        SpreadsheetIds = new HashSet<string>(spreadsheetIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        ChatSpaceIds = new HashSet<string>(chatSpaceIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        ExpenseOrgIds = new HashSet<string>(expenseOrgIds ?? Array.Empty<string>(), StringComparer.Ordinal);
        PaymentAccountIds = new HashSet<string>(paymentAccountIds ?? Array.Empty<string>(), StringComparer.Ordinal);
    }
}' "$force"

  # SeparationOfDutiesRule.cs
  write_file "src/Pipeline.Domain/Security/SeparationOfDutiesRule.cs" 'namespace Pipeline.Domain.Security;

public static class SeparationOfDutiesRule
{
    public static bool IsApproverAllowed(string requesterUserId, string approverUserId)
    {
        if (string.IsNullOrWhiteSpace(requesterUserId) || string.IsNullOrWhiteSpace(approverUserId))
        {
            return false;
        }

        return !string.Equals(requesterUserId.Trim(), approverUserId.Trim(), StringComparison.Ordinal);
    }

    public static bool IsRoleSetValid(IReadOnlySet<string> roleNames)
    {
        if (roleNames is null || roleNames.Count == 0)
        {
            return true;
        }

        var aggregatedPermissions = new HashSet<Permission>();
        var isAuditor = false;

        foreach (var roleName in roleNames)
        {
            if (RoleCatalog.TryGetRole(roleName, out var role) && role is not null)
            {
                if (string.Equals(roleName, RoleCatalog.AuditorRole, StringComparison.OrdinalIgnoreCase))
                {
                    isAuditor = true;
                }

                foreach (var permission in role.Permissions)
                {
                    aggregatedPermissions.Add(permission);
                }
            }
        }

        // Rule 1: No single principal may hold both payments.checkout.create and payments.checkout.approve
        if (aggregatedPermissions.Contains(PermissionCatalog.PaymentsCheckoutCreate) &&
            aggregatedPermissions.Contains(PermissionCatalog.PaymentsCheckoutApprove))
        {
            return false;
        }

        // Rule 2: Auditor can never hold any T2 or T3 write permissions
        if (isAuditor)
        {
            foreach (var permission in aggregatedPermissions)
            {
                if (PermissionCatalog.TryGetTier(permission, out var tier))
                {
                    if (tier is RiskTier.T2 or RiskTier.T3 &&
                        !permission.Value.EndsWith(".read", StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    public static bool IsSelfModificationAttempt(string actingUserId, string targetUserId)
    {
        if (string.IsNullOrWhiteSpace(actingUserId) || string.IsNullOrWhiteSpace(targetUserId))
        {
            return false;
        }

        return string.Equals(actingUserId.Trim(), targetUserId.Trim(), StringComparison.Ordinal);
    }
}' "$force"

  # JobStatus.cs
  write_file "src/Pipeline.Domain/Jobs/JobStatus.cs" 'namespace Pipeline.Domain.Jobs;

public enum JobStatus
{
    Pending,
    Validating,
    Running,
    Failed,
    Succeeded,
    RetryScheduled,
    NeedsReauthorization,
    Cancelled
}' "$force"

  # JobStateMachine.cs
  write_file "src/Pipeline.Domain/Jobs/JobStateMachine.cs" 'using Pipeline.Domain.Common;

namespace Pipeline.Domain.Jobs;

public static class JobStateMachine
{
    public static bool CanTransition(JobStatus from, JobStatus to) =>
        from switch
        {
            JobStatus.Pending => to is JobStatus.Validating or JobStatus.Cancelled,
            JobStatus.Validating => to is JobStatus.Running or JobStatus.Failed or JobStatus.Cancelled,
            JobStatus.Running => to is JobStatus.Succeeded or JobStatus.Failed or JobStatus.RetryScheduled or JobStatus.NeedsReauthorization or JobStatus.Cancelled,
            JobStatus.Failed => to is JobStatus.RetryScheduled,
            JobStatus.RetryScheduled => to is JobStatus.Running or JobStatus.Cancelled,
            JobStatus.NeedsReauthorization => to is JobStatus.Running or JobStatus.Cancelled,
            JobStatus.Succeeded => false,
            JobStatus.Cancelled => false,
            _ => false
        };

    public static Result<JobStatus> Transition(JobStatus from, JobStatus to)
    {
        if (CanTransition(from, to))
        {
            return Result<JobStatus>.Success(to);
        }

        return Result<JobStatus>.Failure(
            "ILLEGAL_STATE_TRANSITION",
            $"Cannot transition job from '\''{from}'\'' to '\''{to}'\''.",
            "STATE_ERR");
    }

    public static bool IsTerminal(JobStatus status) =>
        status is JobStatus.Succeeded or JobStatus.Cancelled;

    public static IReadOnlyList<IReadOnlyList<JobStatus>> GetAllSimplePaths(JobStatus start, JobStatus target)
    {
        var result = new List<IReadOnlyList<JobStatus>>();
        var currentPath = new List<JobStatus> { start };
        var visited = new HashSet<JobStatus> { start };

        void Dfs(JobStatus u)
        {
            if (u == target)
            {
                result.Add(new List<JobStatus>(currentPath));
                return;
            }

            foreach (var next in Enum.GetValues<JobStatus>())
            {
                if (CanTransition(u, next) && !visited.Contains(next))
                {
                    visited.Add(next);
                    currentPath.Add(next);
                    Dfs(next);
                    currentPath.RemoveAt(currentPath.Count - 1);
                    visited.Remove(next);
                }
            }
        }

        Dfs(start);
        return result;
    }
}' "$force"

  # Result.cs
  write_file "src/Pipeline.Domain/Common/Result.cs" 'namespace Pipeline.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }
    public string? PathId { get; }
    public string Outcome => IsSuccess ? "Success" : "Deny";

    protected Result(bool isSuccess, string? errorCode, string? errorMessage, string? pathId)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        PathId = pathId;
    }

    public static Result Success() => new(true, null, null, null);
    public static Result Failure(string errorCode, string errorMessage, string? pathId = null) =>
        new(false, errorCode, errorMessage, pathId);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? errorCode, string? errorMessage, string? pathId)
        : base(isSuccess, errorCode, errorMessage, pathId)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null, null, null);
    public static new Result<T> Failure(string errorCode, string errorMessage, string? pathId = null) =>
        new(false, default, errorCode, errorMessage, pathId);
}' "$force"

  # AuditEntry.cs
  write_file "src/Pipeline.Domain/Common/AuditEntry.cs" 'namespace Pipeline.Domain.Common;

public sealed record AuditEntry(
    string UserId,
    string OperationName,
    string Outcome,
    string ReasonCode,
    string? PathId,
    string CorrelationId,
    string? PrevHash,
    DateTimeOffset TimestampUtc);' "$force"
}

scaffold_application_pipeline() {
  local force="${1:-false}"

  # IUseCaseDispatcher.cs
  write_file "src/Pipeline.Application/Pipeline/IUseCaseDispatcher.cs" 'using Pipeline.Domain.Common;

namespace Pipeline.Application.Pipeline;

public interface IUseCaseDispatcher
{
    Task<Result<TResponse>> DispatchAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default)
        where TRequest : IUseCaseRequest;
}

public interface IUseCaseRequest
{
    string OperationName { get; }
    string? IdempotencyKey => null;
    string? CorrelationId => null;
}' "$force"

  # UseCaseDispatcher.cs
  write_file "src/Pipeline.Application/Pipeline/UseCaseDispatcher.cs" 'using Microsoft.Extensions.DependencyInjection;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Pipeline.Behaviors;
using Pipeline.Application.UseCases;
using Pipeline.Contracts.Errors;
using Pipeline.Domain.Common;

namespace Pipeline.Application.Pipeline;

public sealed class UseCaseDispatcher : IUseCaseDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthenticationBehavior _authnBehavior;
    private readonly ValidationBehavior _validationBehavior;
    private readonly AuthorizationBehavior _authzBehavior;
    private readonly RateLimitBehavior _rateLimitBehavior;
    private readonly IdempotencyBehavior _idempotencyBehavior;
    private readonly AuditBehavior _auditBehavior;
    private readonly IPrincipalAccessor _principalAccessor;

    public UseCaseDispatcher(
        IServiceProvider serviceProvider,
        AuthenticationBehavior authnBehavior,
        ValidationBehavior validationBehavior,
        AuthorizationBehavior authzBehavior,
        RateLimitBehavior rateLimitBehavior,
        IdempotencyBehavior idempotencyBehavior,
        AuditBehavior auditBehavior,
        IPrincipalAccessor principalAccessor)
    {
        _serviceProvider = serviceProvider;
        _authnBehavior = authnBehavior;
        _validationBehavior = validationBehavior;
        _authzBehavior = authzBehavior;
        _rateLimitBehavior = rateLimitBehavior;
        _idempotencyBehavior = idempotencyBehavior;
        _auditBehavior = auditBehavior;
        _principalAccessor = principalAccessor;
    }

    public async Task<Result<TResponse>> DispatchAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken ct = default)
        where TRequest : IUseCaseRequest
    {
        var correlationId = request.CorrelationId ?? Guid.NewGuid().ToString("N");
        var userId = _principalAccessor.Current?.UserId ?? "ANONYMOUS";

        try
        {
            // 1. Authentication
            var authnFailure = _authnBehavior.Check<TResponse>();
            if (authnFailure is not null)
            {
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Deny", authnFailure.ErrorCode!, authnFailure.PathId, correlationId, ct).ConfigureAwait(false);
                return authnFailure;
            }

            // 2. Validation
            var valFailure = _validationBehavior.Validate<TRequest, TResponse>(request);
            if (valFailure is not null)
            {
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Deny", valFailure.ErrorCode!, valFailure.PathId, correlationId, ct).ConfigureAwait(false);
                return valFailure;
            }

            // Resolve Handler
            var handler = _serviceProvider.GetService<IUseCaseHandler<TRequest, TResponse>>();
            if (handler is null)
            {
                var unmapped = Result<TResponse>.Failure(ErrorCodes.OperationUnmapped, $"No handler registered for {typeof(TRequest).Name}", "AUTHZ_P04");
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Deny", unmapped.ErrorCode!, unmapped.PathId, correlationId, ct).ConfigureAwait(false);
                return unmapped;
            }

            // 3. Authorization (S01..S11) -> issues Ticket
            var (authzFailure, ticket) = await _authzBehavior.AuthorizeAsync<TRequest, TResponse>(request, handler.GetType(), ct).ConfigureAwait(false);
            if (authzFailure is not null)
            {
                return authzFailure;
            }

            // 4. Rate Limit
            var rateFailure = await _rateLimitBehavior.CheckRateLimitAsync<TRequest, TResponse>(request, ct).ConfigureAwait(false);
            if (rateFailure is not null)
            {
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Deny", rateFailure.ErrorCode!, rateFailure.PathId, correlationId, ct).ConfigureAwait(false);
                return rateFailure;
            }

            // 5. Idempotency Check
            var fingerprint = AuthorizationBehavior.ComputeFingerprint(request);
            var (existingResult, requiresExecution) = await _idempotencyBehavior.CheckAsync<TRequest, TResponse>(request, fingerprint, ct).ConfigureAwait(false);
            if (!requiresExecution && existingResult is not null)
            {
                return existingResult;
            }

            // 6. Audit Start
            await _auditBehavior.AuditStartAsync(userId, request.OperationName, correlationId, ct).ConfigureAwait(false);

            // 7. Invoke Handler with Capability Ticket & Context
            var context = new UseCaseContext(_principalAccessor.Current!, ticket!, correlationId);
            Result<TResponse> handlerResult;

            try
            {
                handlerResult = await handler.HandleAsync(request, context, ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                await _idempotencyBehavior.MarkUnknownAsync(request.IdempotencyKey, ct).ConfigureAwait(false);
                var timeoutRes = Result<TResponse>.Failure(ErrorCodes.InternalDeny, "Operation timed out with ambiguous outcome.", "IDEM_P05");
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Failure", "TIMEOUT", "IDEM_P05", correlationId, ct).ConfigureAwait(false);
                return timeoutRes;
            }

            // 8. Idempotency Completion & Audit
            if (handlerResult.IsSuccess && handlerResult.Value is not null)
            {
                await _idempotencyBehavior.CompleteAsync(request.IdempotencyKey, handlerResult.Value, ct).ConfigureAwait(false);
            }

            await _auditBehavior.AuditCompletionAsync(
                userId,
                request.OperationName,
                handlerResult.Outcome,
                handlerResult.ErrorCode ?? "OK",
                handlerResult.PathId,
                correlationId,
                ct).ConfigureAwait(false);

            return handlerResult;
        }
        catch (Exception)
        {
            var failRes = Result<TResponse>.Failure(ErrorCodes.InternalDeny, "Internal processing error occurred.", "AUTHZ_PX");
            try
            {
                await _auditBehavior.AuditCompletionAsync(userId, request.OperationName, "Failure", ErrorCodes.InternalDeny, "AUTHZ_PX", correlationId, ct).ConfigureAwait(false);
            }
            catch { }
            return failRes;
        }
    }
}' "$force"

  # UseCaseRegistryValidator.cs
  write_file "src/Pipeline.Application/Pipeline/UseCaseRegistryValidator.cs" 'using System.Reflection;
using Pipeline.Application.Authorization;
using Pipeline.Domain.Security;

namespace Pipeline.Application.Pipeline;

public static class UseCaseRegistryValidator
{
    public static void ValidateOrThrow(IEnumerable<Type> handlerTypes)
    {
        ArgumentNullException.ThrowIfNull(handlerTypes);

        foreach (var (roleName, role) in RoleCatalog.Baseline)
        {
            foreach (var permission in role.Permissions)
            {
                if (!PermissionCatalog.Exists(permission))
                {
                    throw new InvalidOperationException(
                        $"Role '\''{roleName}'\'' references unknown permission '\''{permission.Value}'\''.");
                }
            }
        }

        foreach (var (roleName, role) in RoleCatalog.Baseline)
        {
            if (!SeparationOfDutiesRule.IsRoleSetValid(new HashSet<string> { roleName }))
            {
                throw new InvalidOperationException(
                    $"Role '\''{roleName}'\'' violates Separation of Duties invariants.");
            }
        }

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
                    $"Use case handler '\''{handlerType.FullName}'\'' is missing mandatory [RequiresPermission] attribute.");
            }

            Permission permission;
            try
            {
                permission = new Permission(attr.Permission);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Use case handler '\''{handlerType.FullName}'\'' specifies invalid permission string '\''{attr.Permission}'\''.", ex);
            }

            if (!PermissionCatalog.Exists(permission))
            {
                throw new InvalidOperationException(
                    $"Use case handler '\''{handlerType.FullName}'\'' specifies unknown permission '\''{permission.Value}'\'' not present in PermissionCatalog.");
            }
        }
    }
}' "$force"
}

scaffold_backend_sources() {
  local force="${1:-false}"
  scaffold_backend_projects "$force"
  scaffold_domain_security "$force"
  scaffold_application_pipeline "$force"
}
