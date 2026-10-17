# PipelineSolution: RBAC, Infrastructure Security & Testing Blueprint

> **Purpose:** Extend the `PipelineSolution/` modular monolith with (1) a bypass-resistant RBAC system, (2) infrastructure security controls, and (3) a full test suite covering unit, regression, API, security, and path testing.
>
> **Template convention:** Every `.cs` file below is shown with its **path**, **purpose**, and an **empty body** containing `// GUIDE:` comments. Fill in the logic file by file. Nothing here is final business logic.

---

## Table of Contents

1. [Honest security boundary (read first)](#1-honest-security-boundary-read-first)
2. [RBAC design](#2-rbac-design)
3. [Anti-bypass enforcement chain](#3-anti-bypass-enforcement-chain)
4. [Updated solution structure](#4-updated-solution-structure)
5. [RBAC `.cs` skeletons](#5-rbac-cs-skeletons)
6. [Infrastructure security](#6-infrastructure-security)
7. [Infrastructure `.cs` skeletons](#7-infrastructure-cs-skeletons)
8. [Testing strategy](#8-testing-strategy)
9. [Test projects and `.cs` skeletons](#9-test-projects-and-cs-skeletons)
10. [Path testing](#10-path-testing)
11. [Security test catalog](#11-security-test-catalog)
12. [Regression testing](#12-regression-testing)
13. [CI quality gates](#13-ci-quality-gates)
14. [Implementation order](#14-implementation-order)

---

## 1. Honest security boundary (read first)

"No user can bypass my system" has a precise, achievable meaning. State it correctly in your design docs:

| Attacker | Can RBAC stop them? | How |
|---|---|---|
| App user clicking/abusing the UI | **Yes** | Backend-enforced authorization on every operation |
| App user crafting IPC / localhost requests | **Yes** | Per-launch token, schema validation, same authorization pipeline |
| Another process on the same PC | **Mostly** | Loopback-only, per-launch credential, no generic endpoints |
| Malicious user with a debugger or **local admin** | **No, only raise cost** | In-process code and memory can be altered |
| Role/permission tampering on disk | **Yes, if policy is signed** | Roles come from a **server-signed policy bundle**; the app only verifies |

**Design consequences**

1. **The authority for "who has which role" must not be a local editable file.** Use a signed policy bundle issued by a trusted server (or your IdP's signed token). The desktop app verifies the signature with a pinned public key. If there is no server at all, RBAC is advisory against a determined local admin, and you should document that.
2. **High-value secrets stay off the machine.** PayMongo secret keys and webhook handling belong on a trusted server. The desktop app calls your server, which enforces RBAC again.
3. **Enforce twice.** Client-side pipeline (fast feedback, offline safety) plus server-side enforcement for money and credentials.

---

## 2. RBAC design

### 2.1 Principles

| # | Principle | Meaning |
|---|---|---|
| P1 | **Deny by default** | No permission, no policy, no valid session means Deny. Errors also mean Deny (fail closed). |
| P2 | **Permission-based checks, role-based grants** | Code checks `permission`, never `role == "Admin"`. Roles are only bundles of permissions. |
| P3 | **Single choke point** | Every operation passes through one dispatcher and one authorization behavior. |
| P4 | **Resource-scoped** | Permission alone is not enough. Check *which* spreadsheet, chat space, expense org, or payment account. |
| P5 | **Risk tiers** | Higher-risk operations need fresher authentication and approval. |
| P6 | **Separation of duties (SoD)** | The requester of a Tier 3 action cannot approve it. Security admins cannot operate business actions. |
| P7 | **Capability tickets** | Credentials are released only against a single-use ticket issued by the authorization service. |
| P8 | **Tamper-evident audit** | Every allow and deny decision is written to a hash-chained log. |
| P9 | **Least privilege & expiry** | Sessions, step-up, policy bundles, and tickets all expire. |

### 2.2 Permission naming

Format: `<provider>.<resource>.<action>`

| Permission | Risk tier |
|---|---|
| `jobs.read` | T0 |
| `jobs.cancel` | T1 |
| `sheets.values.read` | T0 |
| `sheets.values.write` | T1 |
| `gmail.messages.send` | T2 |
| `chat.messages.create` | T2 |
| `zoho.expense.read` | T0 |
| `zoho.expense.create` | T2 |
| `bigin.contacts.read` | T0 |
| `bigin.contacts.create` | T2 |
| `payments.status.read` | T0 |
| `payments.checkout.create` | T3 |
| `payments.checkout.approve` | T3 |
| `integrations.connect` | T3 |
| `integrations.disconnect` | T3 |
| `admin.users.manage` | T3 |
| `admin.policy.update` | T3 |
| `admin.audit.read` | T1 |

### 2.3 Risk tiers

| Tier | Definition | Extra requirements |
|---|---|---|
| **T0** | Read-only | Valid session |
| **T1** | Internal state change | Valid session + resource scope |
| **T2** | External side effect (email, chat, record creation) | T1 + step-up freshness (e.g., re-auth within 15 min) or explicit confirm token |
| **T3** | Money, credentials, identity, policy | T2 + **approval** (four-eyes) where configured + shortest ticket TTL |

### 2.4 Role to permission matrix (starting point)

`✓` = granted. Anything not listed is denied.

| Permission | Viewer | Operator | FinanceOperator | Approver | IntegrationAdmin | Auditor | Owner |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| `jobs.read` | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| `jobs.cancel` |  | ✓ | ✓ | ✓ |  |  | ✓ |
| `sheets.values.read` | ✓ | ✓ | ✓ | ✓ |  |  | ✓ |
| `sheets.values.write` |  | ✓ | ✓ |  |  |  | ✓ |
| `gmail.messages.send` |  | ✓ |  |  |  |  | ✓ |
| `chat.messages.create` |  | ✓ |  |  |  |  | ✓ |
| `zoho.expense.read` |  |  | ✓ | ✓ |  |  | ✓ |
| `zoho.expense.create` |  |  | ✓ |  |  |  | ✓ |
| `bigin.contacts.read` | ✓ | ✓ |  |  |  |  | ✓ |
| `bigin.contacts.create` |  | ✓ |  |  |  |  | ✓ |
| `payments.status.read` |  |  | ✓ | ✓ |  |  | ✓ |
| `payments.checkout.create` |  |  | ✓ |  |  |  |  |
| `payments.checkout.approve` |  |  |  | ✓ |  |  |  |
| `integrations.connect` |  |  |  |  | ✓ |  | ✓ |
| `integrations.disconnect` |  |  |  |  | ✓ |  | ✓ |
| `admin.users.manage` |  |  |  |  |  |  | ✓ |
| `admin.policy.update` |  |  |  |  |  |  | ✓ |
| `admin.audit.read` |  |  |  |  |  | ✓ | ✓ |

**SoD rules (encoded in `SeparationOfDutiesRule`)**

- `payments.checkout.create` and `payments.checkout.approve` may not be held by the same principal for the same job.
- `Auditor` can never hold any T2/T3 write permission.
- `Owner` cannot approve their own payment requests (note the Owner row has no payment permissions).
- A principal cannot change their own roles (`admin.users.manage` target is not self).

### 2.5 Authorization decision algorithm

Every operation runs these steps **in order**. The first failure stops evaluation and produces a `Deny` with a reason code. Each step is a *path* you must test (see [Section 10](#10-path-testing)).

```
S01 Principal present and authenticated?                  else DENY  AUTHN_MISSING
S02 Session not expired / revoked / bound to this device?  else DENY  SESSION_INVALID
S03 Policy bundle signature valid, not expired, version
    >= last seen (anti-rollback)?                          else DENY  POLICY_INVALID
S04 Operation declares a required permission?              else DENY  OPERATION_UNMAPPED
S05 Principal holds permission via any role?               else DENY  PERMISSION_MISSING
S06 Resource within principal's scope (account, sheet,
    chat space, org)?                                      else DENY  RESOURCE_OUT_OF_SCOPE
S07 Risk tier >= T2: step-up fresh?                        else CHALLENGE STEPUP_REQUIRED
S08 Risk tier == T3: approval present and valid, and
    approver != requester (SoD)?                           else PENDING / DENY  APPROVAL_REQUIRED
S09 Rate / concurrency limit not exceeded?                 else DENY  RATE_LIMITED
S10 Issue single-use AuthorizationTicket bound to the
    operation fingerprint + short TTL                      => ALLOW
S11 Write audit record for ALLOW / DENY / CHALLENGE        (always, including on failure of any step)
```

---

## 3. Anti-bypass enforcement chain

Bypass is prevented by **structure**, not by remembering to call a check.

```
UI / IPC / LocalApi
      |   (only talks to IUseCaseDispatcher; has no reference to connectors)
      v
IUseCaseDispatcher
      |   pipeline: Authentication -> Validation -> Authorization -> RateLimit
      |             -> Idempotency -> Audit -> Handler
      v
Use case handler           (declares [RequiresPermission], receives AuthorizationTicket)
      |
      v
ICredentialBroker.GetTokenAsync(ticket, provider, account)
      |   verifies ticket: HMAC, unexpired, unused, fingerprint matches, provider matches
      v
Provider connector          (internal class, EgressAllowlistHandler on its HttpClient)
```

### 3.1 Structural controls

| # | Control | What it blocks |
|---|---|---|
| C1 | Provider connectors are `internal`; only the Application layer's handlers can reach them through interfaces. | UI or host calling a connector directly |
| C2 | `ICredentialStore` is `internal` to Infrastructure; the only public way to get a token is `ICredentialBroker`, which **requires** an `AuthorizationTicket`. | A handler or rogue code path skipping authorization |
| C3 | `AuthorizationTicket` has an `internal` constructor. Only `AuthorizationService` creates it. It carries an HMAC with a per-process random key, an operation fingerprint, an expiry, and a single-use nonce. | Forged or replayed tickets |
| C4 | `UseCaseRegistryValidator` runs at startup and **refuses to launch** if any handler lacks `[RequiresPermission]` or lacks a permission in `PermissionCatalog`. | Newly added unprotected operations |
| C5 | Architecture tests (NetArchTest) fail the build if layering or visibility rules are violated. | Developers accidentally opening a bypass |
| C6 | No generic "execute any URL" endpoint; `EgressAllowlistHandler` pins hosts per provider. | SSRF, credential exfiltration |
| C7 | Roles come only from a **signed policy bundle**; role edits require `admin.users.manage` + T3 rules and are audited. | Editing a local file to become Owner |
| C8 | Hash-chained audit log; each record includes the previous record's hash. | Silent deletion or editing of history |
| C9 | IPC messages are schema-validated against a closed set of command types. | Frontend sending arbitrary commands |
| C10 | Per-launch token for any localhost API; `Host` header validation; loopback only. | Other local processes calling the API |

### 3.2 Bypass attempt and what stops it

| Attempt | Stopped by |
|---|---|
| Edit UI state to show an admin screen | S05 on every operation; UI is cosmetic |
| Call an operation with a different spreadsheet ID (IDOR) | S06 resource scope |
| Replay a captured request | Idempotency + single-use ticket nonce |
| Modify the local DB to promote own role | C7 signature verification (S03) |
| Roll back to an older, more permissive policy | S03 monotonic version check |
| Send `url`/`method` to hit another host | C6, no such contract exists |
| Reuse an approved payment request for a different amount | Ticket bound to the operation **fingerprint** (amount is in it) |
| Requester approves own payment | S08 + `SeparationOfDutiesRule` |
| New developer adds an unprotected use case | C4 startup refusal + C5 architecture test |
| Call credential store directly from the host | C2 visibility + C5 test |
| Local process hits localhost API | C10 |
| Tamper with audit entries | C8 chain verification on startup and on read |

---

## 4. Updated solution structure

New or expanded items are marked `(+)`.

```
PipelineSolution/
├── src/
│   ├── Pipeline.Desktop.Host/
│   │   ├── Program.cs
│   │   ├── Composition/
│   │   │   ├── ServiceRegistration.cs
│   │   │   └── StartupValidation.cs                (+)
│   │   ├── Security/                               (+)
│   │   │   ├── WebViewHardening.cs
│   │   │   ├── IpcMessageRouter.cs
│   │   │   └── IpcSchemaValidator.cs
│   │   └── Resources/
│   │
│   ├── Pipeline.Presentation/
│   │   ├── App.tsx
│   │   ├── screens/ components/ services/ contracts/
│   │   └── __tests__/                              (+) Jest + React Testing Library
│   │
│   ├── Pipeline.Domain/
│   │   ├── Jobs/
│   │   │   ├── JobStatus.cs                        (+)
│   │   │   └── JobStateMachine.cs                  (+)
│   │   ├── Security/                               (+)
│   │   │   ├── Permission.cs
│   │   │   ├── PermissionCatalog.cs
│   │   │   ├── RiskTier.cs
│   │   │   ├── Role.cs
│   │   │   ├── RoleCatalog.cs
│   │   │   ├── Principal.cs
│   │   │   ├── ResourceScope.cs
│   │   │   └── SeparationOfDutiesRule.cs
│   │   ├── Integrations/ Policies/ Common/
│   │
│   ├── Pipeline.Application/
│   │   ├── Abstractions/
│   │   │   ├── IJobStore.cs  ICredentialStore.cs  IClock.cs
│   │   │   ├── ICredentialBroker.cs                (+)
│   │   │   ├── IPrincipalAccessor.cs               (+)
│   │   │   ├── IPolicyBundleProvider.cs            (+)
│   │   │   └── IAuditWriter.cs                     (+)
│   │   ├── Authorization/                          (+ expanded)
│   │   │   ├── IAuthorizationService.cs
│   │   │   ├── AuthorizationService.cs
│   │   │   ├── AuthorizationDecision.cs
│   │   │   ├── AuthorizationTicket.cs
│   │   │   ├── RequiresPermissionAttribute.cs
│   │   │   ├── IResourceAuthorizer.cs
│   │   │   ├── IApprovalService.cs
│   │   │   └── IStepUpAuthenticator.cs
│   │   ├── Pipeline/                               (+)
│   │   │   ├── IUseCaseDispatcher.cs
│   │   │   ├── UseCaseDispatcher.cs
│   │   │   ├── Behaviors/
│   │   │   │   ├── AuthenticationBehavior.cs
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── AuthorizationBehavior.cs
│   │   │   │   ├── RateLimitBehavior.cs
│   │   │   │   ├── IdempotencyBehavior.cs
│   │   │   │   └── AuditBehavior.cs
│   │   │   └── UseCaseRegistryValidator.cs
│   │   ├── UseCases/ Validation/ Orchestration/
│   │
│   ├── Pipeline.Contracts/  Requests/ Responses/ Errors/
│   │
│   ├── Pipeline.Infrastructure/
│   │   ├── Persistence/ Resilience/ Observability/
│   │   ├── Http/
│   │   │   └── EgressAllowlistHandler.cs           (+)
│   │   └── Security/                               (+ expanded)
│   │       ├── DpapiCredentialStore.cs
│   │       ├── CredentialBroker.cs
│   │       ├── SignedPolicyBundleVerifier.cs
│   │       ├── SessionManager.cs
│   │       ├── HashChainAuditWriter.cs
│   │       ├── LocalApiTokenService.cs
│   │       └── RedactionPolicy.cs
│   │
│   ├── Integrations.Google.Sheets/ ... Integrations.PayMongo/
│   └── Pipeline.LocalApi/
│       └── Security/                               (+)
│           ├── LoopbackOnlyMiddleware.cs
│           ├── HostHeaderMiddleware.cs
│           ├── PerLaunchTokenAuthHandler.cs
│           └── SecurityHeadersMiddleware.cs
│
└── tests/
    ├── Pipeline.TestKit/                           (+) shared builders and fakes
    ├── Pipeline.Domain.Tests/                      unit
    ├── Pipeline.Application.Tests/                 unit + authorization matrix + paths
    ├── Pipeline.Integration.Tests/                 SQLite, WireMock, sandbox
    ├── Pipeline.Contract.Tests/                    provider + API contracts
    ├── Pipeline.Api.Tests/                         (+) LocalApi endpoint tests
    ├── Pipeline.Security.Tests/                    (+) bypass and abuse tests
    ├── Pipeline.Architecture.Tests/                (+) structural rules
    └── Pipeline.Regression.Tests/                  (+) bug-fix and snapshot tests
```

---

## 5. RBAC `.cs` skeletons

> All bodies are intentionally empty. Follow the `// GUIDE:` comments.

### 5.1 Domain / Security

#### `src/Pipeline.Domain/Security/Permission.cs`
```csharp
namespace Pipeline.Domain.Security;

// GUIDE: Immutable value object. Format "<provider>.<resource>.<action>", lowercase.
// GUIDE: Validate format in the constructor (regex ^[a-z]+(\.[a-z]+){2}$). Reject anything else.
public readonly record struct Permission
{
    public string Value { get; }
    public Permission(string value) { /* TODO: validate + assign */ }
}
```

#### `src/Pipeline.Domain/Security/PermissionCatalog.cs`
```csharp
namespace Pipeline.Domain.Security;

// GUIDE: The ONLY place permissions are declared. Section 2.2 table maps 1:1 to these entries.
// GUIDE: Each entry = (Permission, RiskTier). Expose All for tests and startup validation.
public static class PermissionCatalog
{
    // public static readonly Permission SheetsValuesRead = new("sheets.values.read");
    // TODO: declare every permission from Section 2.2

    public static IReadOnlyDictionary<Permission, RiskTier> All { get; } = default!; // TODO
}
```

#### `src/Pipeline.Domain/Security/RiskTier.cs`
```csharp
namespace Pipeline.Domain.Security;

// GUIDE: T0 read, T1 internal change, T2 external side effect, T3 money/credentials/identity/policy.
public enum RiskTier { T0 = 0, T1 = 1, T2 = 2, T3 = 3 }
```

#### `src/Pipeline.Domain/Security/Role.cs`
```csharp
namespace Pipeline.Domain.Security;

// GUIDE: A named bundle of permissions. Never branch on Role in business code (Principle P2).
public sealed record Role(string Name, IReadOnlySet<Permission> Permissions);
```

#### `src/Pipeline.Domain/Security/RoleCatalog.cs`
```csharp
namespace Pipeline.Domain.Security;

// GUIDE: Static baseline of roles from the Section 2.4 matrix.
// GUIDE: The signed policy bundle may only reference role names that exist here and may only
//        grant permissions that exist in PermissionCatalog. Unknown names => reject bundle.
public static class RoleCatalog
{
    public static IReadOnlyDictionary<string, Role> Baseline { get; } = default!; // TODO
}
```

#### `src/Pipeline.Domain/Security/Principal.cs`
```csharp
namespace Pipeline.Domain.Security;

// GUIDE: Authenticated identity for the current session. Immutable. Built ONLY by SessionManager
//        after verifying the policy bundle. No public setters; no deserialization from UI input.
public sealed class Principal
{
    public string UserId { get; init; } = default!;
    public IReadOnlySet<string> RoleNames { get; init; } = default!;
    public ResourceScope Scope { get; init; } = default!;
    public DateTimeOffset SessionExpiresAtUtc { get; init; }
    public DateTimeOffset? LastStepUpAtUtc { get; init; }

    public bool Has(Permission permission) => throw new NotImplementedException();
    // GUIDE: Has = any role in RoleNames whose Role.Permissions contains permission.
}
```

#### `src/Pipeline.Domain/Security/ResourceScope.cs`
```csharp
namespace Pipeline.Domain.Security;

// GUIDE: Allow-lists of concrete resources a principal may touch (P4).
// GUIDE: Empty list = nothing allowed (not "everything allowed").
public sealed class ResourceScope
{
    public IReadOnlySet<string> SpreadsheetIds { get; init; } = default!;
    public IReadOnlySet<string> ChatSpaceIds { get; init; } = default!;
    public IReadOnlySet<string> ZohoOrganizationIds { get; init; } = default!;
    public IReadOnlySet<string> PaymentAccountIds { get; init; } = default!;
}
```

#### `src/Pipeline.Domain/Security/SeparationOfDutiesRule.cs`
```csharp
namespace Pipeline.Domain.Security;

// GUIDE: Pure functions only (no I/O) so they are trivially unit-testable.
public static class SeparationOfDutiesRule
{
    public static bool IsApproverAllowed(string requesterUserId, string approverUserId)
        => throw new NotImplementedException(); // GUIDE: false when equal (ordinal, case-sensitive)

    public static bool IsRoleSetValid(IReadOnlySet<string> roleNames)
        => throw new NotImplementedException(); // GUIDE: enforce Section 2.4 SoD bullets
}
```

### 5.2 Application / Authorization

#### `src/Pipeline.Application/Authorization/RequiresPermissionAttribute.cs`
```csharp
namespace Pipeline.Application.Authorization;

// GUIDE: Applied to every use-case handler class. Missing attribute => UseCaseRegistryValidator
//        aborts startup (control C4).
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequiresPermissionAttribute : Attribute
{
    public string Permission { get; }
    public RequiresPermissionAttribute(string permission) => Permission = permission;
}
```

#### `src/Pipeline.Application/Authorization/AuthorizationDecision.cs`
```csharp
namespace Pipeline.Application.Authorization;

// GUIDE: Result of S01-S10. Carries a stable reason code (never raw exception text) for audit/UI.
public enum DecisionOutcome { Allow, Deny, Challenge, PendingApproval }

public sealed record AuthorizationDecision(
    DecisionOutcome Outcome,
    string ReasonCode,                 // e.g., PERMISSION_MISSING, STEPUP_REQUIRED
    string? PathId,                    // e.g., "AUTHZ_P05", used by path-coverage tests
    AuthorizationTicket? Ticket);      // non-null only when Outcome == Allow
```

#### `src/Pipeline.Application/Authorization/AuthorizationTicket.cs`
```csharp
namespace Pipeline.Application.Authorization;

// GUIDE: Capability token (control C3). Constructor is INTERNAL; only AuthorizationService creates it.
// GUIDE: Fields: Nonce (random 128-bit), OperationName, OperationFingerprint (hash of the
//        normalized request incl. amounts/recipients), Provider, AccountId, ExpiresAtUtc, Mac.
// GUIDE: Mac = HMAC-SHA256 over all fields using a per-process random key held ONLY by
//        the authorization service and the credential broker (never persisted).
public sealed class AuthorizationTicket
{
    internal AuthorizationTicket(/* TODO: fields */) { }

    public string Nonce { get; } = default!;
    public string OperationFingerprint { get; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; }
}
```

#### `src/Pipeline.Application/Authorization/IAuthorizationService.cs`
```csharp
namespace Pipeline.Application.Authorization;

public interface IAuthorizationService
{
    // GUIDE: Implements S01-S11. Never throws for deny; returns a decision. Throws only on
    //        programmer error, which the dispatcher converts to DENY (fail closed).
    Task<AuthorizationDecision> AuthorizeAsync(
        AuthorizationRequest request, CancellationToken ct);
}

// GUIDE: What the dispatcher passes in. Built by the pipeline, never by the caller.
public sealed record AuthorizationRequest(
    string OperationName,
    string RequiredPermission,
    string OperationFingerprint,
    string? Provider,
    string? AccountId,
    IReadOnlyDictionary<string, string> ResourceIds,   // e.g., {"spreadsheetId": "..."}
    string? ApprovalId);
```

#### `src/Pipeline.Application/Authorization/AuthorizationService.cs`
```csharp
namespace Pipeline.Application.Authorization;

// GUIDE: Constructor deps: IPrincipalAccessor, IPolicyBundleProvider, IResourceAuthorizer,
//        IStepUpAuthenticator, IApprovalService, IClock, IAuditWriter, rate limiter.
// GUIDE: Implement each step as a small private method returning a nullable Deny decision,
//        so every path has an ID (AUTHZ_P01..P10) and can be tested in isolation.
// GUIDE: ALWAYS write audit (S11) in a finally block, including for exceptions.
internal sealed class AuthorizationService : IAuthorizationService
{
    public Task<AuthorizationDecision> AuthorizeAsync(AuthorizationRequest request, CancellationToken ct)
        => throw new NotImplementedException();

    // private AuthorizationDecision? CheckAuthenticated()      // S01
    // private AuthorizationDecision? CheckSession()            // S02
    // private AuthorizationDecision? CheckPolicyBundle()       // S03
    // private AuthorizationDecision? CheckOperationMapped()    // S04
    // private AuthorizationDecision? CheckPermission()         // S05
    // private Task<AuthorizationDecision?> CheckResourceScopeAsync() // S06
    // private AuthorizationDecision? CheckStepUp()             // S07
    // private Task<AuthorizationDecision?> CheckApprovalAsync()// S08
    // private AuthorizationDecision? CheckRateLimit()          // S09
    // private AuthorizationTicket IssueTicket()                // S10
}
```

#### `src/Pipeline.Application/Authorization/IResourceAuthorizer.cs`
```csharp
namespace Pipeline.Application.Authorization;

// GUIDE: One implementation per resource family (Sheets, Chat, Zoho, Payments) so scope rules
//        stay with their provider semantics. Compare IDs with ordinal, case-sensitive equality
//        after canonicalization (trim, reject control chars).
public interface IResourceAuthorizer
{
    bool CanHandle(string provider);
    bool IsInScope(Principal principal, IReadOnlyDictionary<string, string> resourceIds);
}
```

#### `src/Pipeline.Application/Authorization/IApprovalService.cs`
```csharp
namespace Pipeline.Application.Authorization;

// GUIDE: Approvals are persisted, bound to OperationFingerprint, single-use, and expire.
// GUIDE: Verify approver != requester using SeparationOfDutiesRule.
public interface IApprovalService
{
    Task<ApprovalResult> ValidateAsync(string approvalId, string operationFingerprint,
        string requesterUserId, CancellationToken ct);
    Task<string> RequestAsync(string operationFingerprint, string requesterUserId, CancellationToken ct);
}

public sealed record ApprovalResult(bool IsValid, string ReasonCode);
```

#### `src/Pipeline.Application/Authorization/IStepUpAuthenticator.cs`
```csharp
namespace Pipeline.Application.Authorization;

// GUIDE: Re-authentication (OIDC prompt=login / Windows Hello / PIN). Updates Principal.LastStepUpAtUtc
//        via SessionManager. Freshness window per tier configured centrally (e.g., T2=15m, T3=5m).
public interface IStepUpAuthenticator
{
    bool IsFresh(Principal principal, RiskTier tier, DateTimeOffset nowUtc);
    Task<bool> ChallengeAsync(CancellationToken ct);
}
```

### 5.3 Application / Pipeline (single choke point)

#### `src/Pipeline.Application/Pipeline/IUseCaseDispatcher.cs`
```csharp
namespace Pipeline.Application.Pipeline;

// GUIDE: The ONLY entry point from UI/IPC/LocalApi into business logic (control C1).
public interface IUseCaseDispatcher
{
    Task<Result<TResponse>> DispatchAsync<TRequest, TResponse>(TRequest request, CancellationToken ct)
        where TRequest : IUseCaseRequest;
}

public interface IUseCaseRequest { string OperationName { get; } }
```

#### `src/Pipeline.Application/Pipeline/UseCaseDispatcher.cs`
```csharp
namespace Pipeline.Application.Pipeline;

// GUIDE: Resolve handler; read [RequiresPermission]; build the behavior chain in FIXED order:
//        Authentication -> Validation -> Authorization -> RateLimit -> Idempotency -> Audit -> Handler.
// GUIDE: Wrap in try/catch: any unexpected exception => Deny-style failure + audit; never leak details.
// GUIDE: Behavior order must not be configurable from outside this class.
internal sealed class UseCaseDispatcher : IUseCaseDispatcher
{
    public Task<Result<TResponse>> DispatchAsync<TRequest, TResponse>(TRequest request, CancellationToken ct)
        where TRequest : IUseCaseRequest
        => throw new NotImplementedException();
}
```

#### `src/Pipeline.Application/Pipeline/Behaviors/AuthorizationBehavior.cs`
```csharp
namespace Pipeline.Application.Pipeline.Behaviors;

// GUIDE: Builds AuthorizationRequest from the use-case request (fingerprint = SHA-256 of canonical JSON
//        of security-relevant fields). Calls IAuthorizationService.
// GUIDE: Outcome handling: Allow => put Ticket into the handler context; Deny => 403 result;
//        Challenge => result telling UI to step up; PendingApproval => 202 + approval id.
// GUIDE: The handler receives the ticket via context; it can pass it ONLY to ICredentialBroker.
internal sealed class AuthorizationBehavior { /* TODO */ }
```

#### `src/Pipeline.Application/Pipeline/Behaviors/` (remaining behaviors)
```csharp
// AuthenticationBehavior.cs  GUIDE: Reject if IPrincipalAccessor.Current is null/expired. No anonymous path exists.
// ValidationBehavior.cs      GUIDE: Run request validators BEFORE authorization so malformed input is rejected
//                                  cheaply, but never reveal whether a resource exists (avoid oracle).
// RateLimitBehavior.cs       GUIDE: Per-principal and per-provider token buckets; honor provider Retry-After.
// IdempotencyBehavior.cs     GUIDE: Look up (OperationName, IdempotencyKey, Fingerprint). Same key + different
//                                  fingerprint => 409. Same key + completed => return stored result.
// AuditBehavior.cs           GUIDE: Write start/finish records with JobId, CorrelationId, outcome code. No payloads.
```

#### `src/Pipeline.Application/Pipeline/UseCaseRegistryValidator.cs`
```csharp
namespace Pipeline.Application.Pipeline;

// GUIDE: Runs at startup (StartupValidation.cs). Scan all handlers:
//   - missing [RequiresPermission]            => throw (app does not start)
//   - permission not in PermissionCatalog     => throw
//   - handler uses a connector type directly  => (covered by architecture tests)
// GUIDE: Also verify RoleCatalog only references catalog permissions, and SoD rules hold for all roles.
public static class UseCaseRegistryValidator
{
    public static void ValidateOrThrow(IEnumerable<Type> handlerTypes) => throw new NotImplementedException();
}
```

---

## 6. Infrastructure security

### 6.1 Threat model summary (STRIDE)

| Threat | Example | Primary control |
|---|---|---|
| **S**poofing | Fake session / forged role | Signed policy bundle (S03), OIDC + PKCE, session binding |
| **T**ampering | Edit DB, roles, audit log, config | Signature verification, hash-chained audit, DB file ACL, signed binaries |
| **R**epudiation | "I never sent that email" | Audit with UserId, JobId, CorrelationId, decision reason |
| **I**nformation disclosure | Token in logs / React state / API response | DPAPI store, `RedactionPolicy`, no token ever crosses IPC |
| **D**enial of service | Spamming operations | Rate limit, per-account concurrency cap, request size limits |
| **E**levation of privilege | Operator reaches payments | Permission checks, resource scope, SoD, capability tickets |

### 6.2 Control catalog

| Area | Control | Notes |
|---|---|---|
| **Identity** | OIDC Authorization Code + PKCE via the system browser | Never embed a client secret in the app. Tokens held by the host only. |
| **Policy** | Signed policy bundle (ECDSA P-256 or Ed25519), public key pinned in the binary | Includes `Version` (monotonic), `ExpiresAtUtc`, user to roles, resource scopes. Offline grace period is configurable and short. |
| **Session** | `SessionManager` creates `Principal` after verifying bundle + identity token | Sliding idle timeout and absolute lifetime. Lock/revoke on bundle change. |
| **Credentials at rest** | Windows DPAPI (`ProtectedData`, `CurrentUser` scope) with optional app-specific entropy, or Credential Manager | Refresh tokens only. Access tokens kept in memory. |
| **Credential release** | `ICredentialBroker` requires a valid `AuthorizationTicket` | Per-account `SemaphoreSlim` prevents refresh races. |
| **Database** | SQLite in `%LOCALAPPDATA%\<App>\` with restrictive ACL; SQLCipher or DPAPI-wrapped key | Parameterized queries only (no string concatenation). Migrations versioned. |
| **Audit** | Append-only table; each row stores `PrevHash` and `Hash = SHA256(PrevHash + canonical row)` | Verify chain on startup and on audit read; surface tampering as a blocking alert. |
| **Egress** | `EgressAllowlistHandler` on every provider `HttpClient` | HTTPS only; host allowlist per provider; `AllowAutoRedirect=false`; redirects revalidated; TLS 1.2+. |
| **Local API (optional)** | Bind `127.0.0.1`, ephemeral port, per-launch random token, `Host` validation, no CORS | If in-process IPC is enough, **do not open a port**. |
| **WebView2 IPC** | Closed command set, JSON schema validation, origin check, size limit | Disable DevTools, context menu, and external navigation in Release builds. |
| **Frontend** | Strict CSP, no inline script, assets loaded from app-mapped virtual host | No secrets in bundle or `localStorage`. |
| **Logging** | `RedactionPolicy` strips `Authorization`, tokens, emails (optional mask), card or account data | Audit log kept separate from diagnostic log. |
| **Configuration** | Typed options, validated at startup; no secrets in `appsettings.json` | Fail fast on invalid config. |
| **Supply chain** | `RestoreLockedMode`, NuGet package-source mapping, vulnerability audit, SBOM, secret scanning | Gate in CI ([Section 13](#13-ci-quality-gates)). |
| **Release** | Authenticode signing, trusted update channel with signature verification | Never auto-update from an unsigned or plain-HTTP source. |
| **Payments** | Secret-key operations and webhooks on a **trusted server only** | Desktop calls your server with its own RBAC enforcement. |

### 6.3 Secure defaults checklist

- [ ] Every outbound `HttpClient` is created through `IHttpClientFactory` with `EgressAllowlistHandler`.
- [ ] No class outside `Pipeline.Infrastructure.Security` references `DpapiCredentialStore`.
- [ ] `JsonSerializerOptions` for IPC/API: unknown properties rejected where practical, max depth set.
- [ ] Exceptions are mapped to stable error codes; stack traces never leave the process.
- [ ] Release build: `Debugger` attach detection logged (best-effort), DevTools disabled.
- [ ] Time comes from `IClock`; clock rollback on session/bundle checks is detected (compare against last-seen UTC).

---

## 7. Infrastructure `.cs` skeletons

### 7.1 Application abstractions

#### `src/Pipeline.Application/Abstractions/ICredentialBroker.cs`
```csharp
namespace Pipeline.Application.Abstractions;

// GUIDE: The ONLY public way to obtain a provider token (control C2).
// GUIDE: Implementation validates the ticket (MAC, expiry, single-use nonce, fingerprint, provider)
//        before touching ICredentialStore. Failure => throws CredentialAccessDeniedException + audit.
public interface ICredentialBroker
{
    Task<AccessToken> GetAccessTokenAsync(
        AuthorizationTicket ticket, string provider, string accountId, CancellationToken ct);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc); // GUIDE: override ToString() to redact
```

#### `src/Pipeline.Application/Abstractions/IPrincipalAccessor.cs`
```csharp
namespace Pipeline.Application.Abstractions;

// GUIDE: Read-only view of the current Principal. Only SessionManager can set it.
public interface IPrincipalAccessor { Principal? Current { get; } }
```

#### `src/Pipeline.Application/Abstractions/IPolicyBundleProvider.cs`
```csharp
namespace Pipeline.Application.Abstractions;

// GUIDE: Returns the last VERIFIED bundle. Never returns an unverified or expired bundle.
public interface IPolicyBundleProvider
{
    PolicyBundle? Current { get; }
    Task<bool> RefreshAsync(CancellationToken ct);
}

public sealed record PolicyBundle(long Version, DateTimeOffset ExpiresAtUtc /* TODO: users, roles, scopes */);
```

#### `src/Pipeline.Application/Abstractions/IAuditWriter.cs`
```csharp
namespace Pipeline.Application.Abstractions;

// GUIDE: Append-only. No update/delete methods exist, by design.
public interface IAuditWriter
{
    Task WriteAsync(AuditEntry entry, CancellationToken ct);
    Task<AuditChainStatus> VerifyChainAsync(CancellationToken ct);
}

public sealed record AuditEntry(
    string UserId, string OperationName, string Outcome, string ReasonCode,
    string? PathId, string CorrelationId, string? JobId, DateTimeOffset TimestampUtc);

public sealed record AuditChainStatus(bool IsIntact, long? FirstBrokenSequence);
```

### 7.2 Infrastructure / Security

#### `src/Pipeline.Infrastructure/Security/DpapiCredentialStore.cs`
```csharp
namespace Pipeline.Infrastructure.Security;

// GUIDE: INTERNAL. Implements ICredentialStore. Use ProtectedData.Protect(..., DataProtectionScope.CurrentUser).
// GUIDE: Store only refresh tokens, keyed by (provider, accountId). Zero byte arrays after use.
// GUIDE: Per-(provider, account) SemaphoreSlim guards refresh-and-write so jobs cannot race.
internal sealed class DpapiCredentialStore : ICredentialStore { /* TODO */ }
```

#### `src/Pipeline.Infrastructure/Security/CredentialBroker.cs`
```csharp
namespace Pipeline.Infrastructure.Security;

// GUIDE: Validate ticket in this order: MAC => expiry => nonce unused (atomic mark-used) =>
//        provider/account match => fingerprint present. Any failure => deny + audit "CREDENTIAL_DENIED".
// GUIDE: The MAC key is shared with AuthorizationService via an internal TicketKeyProvider
//        (random per process, never written to disk or logged).
internal sealed class CredentialBroker : ICredentialBroker
{
    public Task<AccessToken> GetAccessTokenAsync(AuthorizationTicket t, string provider, string account, CancellationToken ct)
        => throw new NotImplementedException();
}
```

#### `src/Pipeline.Infrastructure/Security/SignedPolicyBundleVerifier.cs`
```csharp
namespace Pipeline.Infrastructure.Security;

// GUIDE: Verify detached signature over canonical bytes using the pinned public key.
// GUIDE: Reject if: bad signature | expired | Version < LastSeenVersion (rollback) |
//        unknown role/permission names | SoD violations in role assignments.
// GUIDE: Persist LastSeenVersion in DPAPI-protected storage.
internal sealed class SignedPolicyBundleVerifier { /* TODO */ }
```

#### `src/Pipeline.Infrastructure/Security/SessionManager.cs`
```csharp
namespace Pipeline.Infrastructure.Security;

// GUIDE: Builds Principal from verified identity token + verified bundle. Implements IPrincipalAccessor.
// GUIDE: Enforce idle timeout + absolute lifetime; on bundle version change, rebuild or revoke Principal.
// GUIDE: Record step-up completion by replacing Principal with a new instance (immutability).
internal sealed class SessionManager : IPrincipalAccessor { public Principal? Current => throw new NotImplementedException(); }
```

#### `src/Pipeline.Infrastructure/Security/HashChainAuditWriter.cs`
```csharp
namespace Pipeline.Infrastructure.Security;

// GUIDE: Hash = SHA256(PrevHash || canonical(entry)). Insert + read previous hash inside one DB transaction.
// GUIDE: Canonicalization must be deterministic (fixed field order, UTC ISO-8601, no locale).
// GUIDE: VerifyChainAsync recomputes the chain and reports the first broken sequence.
internal sealed class HashChainAuditWriter : IAuditWriter { /* TODO */ }
```

#### `src/Pipeline.Infrastructure/Security/LocalApiTokenService.cs`
```csharp
namespace Pipeline.Infrastructure.Security;

// GUIDE: Generate 256-bit random token at launch (RandomNumberGenerator). Deliver to the WebView via
//        trusted native path only. Compare with CryptographicOperations.FixedTimeEquals.
internal sealed class LocalApiTokenService { /* TODO */ }
```

#### `src/Pipeline.Infrastructure/Security/RedactionPolicy.cs`
```csharp
namespace Pipeline.Infrastructure.Security;

// GUIDE: Used by logging enrichers and by exception mappers. Redact header names:
//        Authorization, Cookie, X-Api-Key; JSON fields: access_token, refresh_token, client_secret, password.
public static class RedactionPolicy { public static string Redact(string input) => throw new NotImplementedException(); }
```

#### `src/Pipeline.Infrastructure/Http/EgressAllowlistHandler.cs`
```csharp
namespace Pipeline.Infrastructure.Http;

// GUIDE: DelegatingHandler. Constructed per provider with its allowed hosts, e.g.
//        Sheets: sheets.googleapis.com | Gmail: gmail.googleapis.com | Chat: chat.googleapis.com
//        Zoho: region-specific hosts | PayMongo: api.paymongo.com. Verify against official docs.
// GUIDE: Reject: non-HTTPS, host not in list, IP-literal hosts, userinfo in URL, redirects to other hosts.
// GUIDE: Strip any caller-supplied Authorization header; auth is added only by the connector.
internal sealed class EgressAllowlistHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => throw new NotImplementedException();
}
```

### 7.3 Desktop host and LocalApi

#### `src/Pipeline.Desktop.Host/Security/WebViewHardening.cs`
```csharp
namespace Pipeline.Desktop.Host.Security;

// GUIDE: Apply once after WebView2 init. Release builds: disable DevTools, context menu, status bar;
//        block NavigationStarting to any origin except the app's virtual host; deny NewWindowRequested.
// GUIDE: Use SetVirtualHostNameToFolderMapping (or embedded resources) so no file:// access is needed.
internal static class WebViewHardening { public static void Apply(/* CoreWebView2 */) { /* TODO */ } }
```

#### `src/Pipeline.Desktop.Host/Security/IpcMessageRouter.cs`
```csharp
namespace Pipeline.Desktop.Host.Security;

// GUIDE: On WebMessageReceived: verify Source == app origin, size <= limit, validate envelope
//        {id, command, payload}. Command must exist in a CLOSED map: commandName => (requestType, handler).
// GUIDE: Route ONLY through IUseCaseDispatcher. There is no "run arbitrary operation" command.
internal sealed class IpcMessageRouter { /* TODO */ }
```

#### `src/Pipeline.Desktop.Host/Security/IpcSchemaValidator.cs`
```csharp
namespace Pipeline.Desktop.Host.Security;

// GUIDE: Per-command JSON schema or strongly-typed deserialization with strict options.
//        Reject unknown fields for T1+ commands. Return stable error codes, not parser exceptions.
internal sealed class IpcSchemaValidator { /* TODO */ }
```

#### `src/Pipeline.Desktop.Host/Composition/StartupValidation.cs`
```csharp
namespace Pipeline.Desktop.Host.Composition;

// GUIDE: Executed before the window opens. Fail closed. Steps:
//   1) Validate options/config  2) UseCaseRegistryValidator  3) Verify policy bundle
//   4) IAuditWriter.VerifyChainAsync  5) DB migrations  6) Egress allowlist sanity check.
internal static class StartupValidation { public static Task RunAsync(IServiceProvider sp) => throw new NotImplementedException(); }
```

#### `src/Pipeline.LocalApi/Security/` (all four files)
```csharp
// LoopbackOnlyMiddleware.cs      GUIDE: Reject if HttpContext.Connection.RemoteIpAddress is not loopback => 403.
// HostHeaderMiddleware.cs        GUIDE: Allow only "127.0.0.1:{port}" / "localhost:{port}" (DNS-rebinding defense) => 400.
// PerLaunchTokenAuthHandler.cs   GUIDE: Authorization: Bearer <launch-token>; FixedTimeEquals; 401 otherwise.
//                                       Token authenticates the CALLER PROCESS only; user authorization still runs
//                                       in IUseCaseDispatcher. Never treat this token as user identity.
// SecurityHeadersMiddleware.cs   GUIDE: Cache-Control: no-store; X-Content-Type-Options: nosniff;
//                                       no Access-Control-Allow-* headers at all.
```

---

## 8. Testing strategy

### 8.1 Test types and where they live

| Test type | Project | Tooling (suggested) | Network? | Runs on |
|---|---|---|---|---|
| **Unit** | `Domain.Tests`, `Application.Tests` | xUnit, NSubstitute, Shouldly (or AwesomeAssertions), Bogus | No | Every commit |
| **Property-based** | `Domain.Tests`, `Application.Tests` | FsCheck.Xunit | No | Every commit |
| **Path (basis-path)** | `Application.Tests`, `Domain.Tests` | xUnit Theory + path IDs | No | Every commit |
| **Architecture** | `Architecture.Tests` | NetArchTest.Rules | No | Every commit |
| **API** | `Api.Tests` | `WebApplicationFactory`, `HttpClient` | In-process | Every commit |
| **Integration** | `Integration.Tests` | SQLite in-memory/file, WireMock.Net | Local stubs | Every commit |
| **Contract** | `Contract.Tests` | Verify (snapshots), JSON Schema / OpenAPI diff | No | Every commit |
| **Security** | `Security.Tests` | xUnit + abuse-case data | In-process | Every commit |
| **Regression** | `Regression.Tests` | xUnit + Verify snapshots | No | Every commit |
| **Sandbox / provider E2E** | `Integration.Tests` (`Category=Sandbox`) | Real test accounts | Yes | Nightly / manual |
| **Mutation** | all unit projects | Stryker.NET | No | Nightly |
| **Frontend** | `Pipeline.Presentation/__tests__` | Jest + React Testing Library | No | Every commit |

### 8.2 Traits (so CI can filter)

Every test class carries exactly one category trait:

```csharp
[Trait("Category", "Unit")]        // Unit | Path | Architecture | Api | Integration | Contract
[Trait("Category", "Security")]    // Security | Regression | Sandbox
```

Run examples:

```
dotnet test --filter "Category=Security"
dotnet test --filter "Category!=Sandbox"
```

### 8.3 Naming conventions

| Item | Convention |
|---|---|
| Test method | `Method_Scenario_ExpectedResult` |
| Path test | `AUTHZ_P05_PermissionMissing_Denies` (path ID prefix) |
| Security test | `SEC_<AreaCode>_<Attack>_<Expected>`, e.g., `SEC_RBAC_OperatorCallsPaymentsCreate_Denied` |
| Regression test | `REG_<IssueId>_<ShortDescription>` |
| Snapshot file | Stored under `Snapshots/`, reviewed like code (CODEOWNERS for security snapshots) |

---

## 9. Test projects and `.cs` skeletons

### 9.1 Tree with files

```
tests/
├── Pipeline.TestKit/
│   ├── Builders/PrincipalBuilder.cs
│   ├── Builders/PolicyBundleFactory.cs
│   ├── Fakes/FakeClock.cs
│   ├── Fakes/InMemoryJobStore.cs
│   ├── Fakes/InMemoryAuditWriter.cs
│   └── Fixtures/WireMockFixture.cs
│
├── Pipeline.Domain.Tests/
│   ├── Jobs/JobStateMachineTests.cs
│   ├── Jobs/JobStateMachinePathTests.cs
│   ├── Security/PermissionTests.cs
│   ├── Security/RoleCatalogTests.cs
│   ├── Security/PrincipalTests.cs
│   └── Security/SeparationOfDutiesRuleTests.cs
│
├── Pipeline.Application.Tests/
│   ├── Authorization/AuthorizationMatrixTests.cs
│   ├── Authorization/AuthorizationDecisionPathTests.cs
│   ├── Authorization/AuthorizationTicketTests.cs
│   ├── Pipeline/DispatcherOrderTests.cs
│   ├── Pipeline/IdempotencyBehaviorTests.cs
│   ├── Pipeline/UseCaseRegistryValidatorTests.cs
│   └── UseCases/                         (one file per use case)
│
├── Pipeline.Integration.Tests/
│   ├── Persistence/SqliteJobStoreTests.cs
│   ├── Persistence/HashChainAuditWriterTests.cs
│   ├── Security/DpapiCredentialStoreTests.cs
│   ├── Http/EgressAllowlistHandlerTests.cs
│   └── Providers/{Sheets,Gmail,Chat,ZohoExpense,Bigin,PayMongo}/
│       ├── *WireMockTests.cs             (always on)
│       └── *SandboxTests.cs              (Category=Sandbox)
│
├── Pipeline.Contract.Tests/
│   ├── Providers/SheetsRequestContractTests.cs   (+ one per provider)
│   ├── Api/ErrorContractTests.cs
│   ├── Api/OpenApiSnapshotTests.cs
│   └── Snapshots/
│
├── Pipeline.Api.Tests/
│   ├── Infrastructure/LocalApiFactory.cs
│   ├── Endpoints/JobsEndpointTests.cs
│   ├── Endpoints/IntegrationsEndpointTests.cs
│   ├── Endpoints/StatusCodeMatrixTests.cs
│   └── Endpoints/ProblemDetailsTests.cs
│
├── Pipeline.Security.Tests/
│   ├── Rbac/PrivilegeEscalationTests.cs
│   ├── Rbac/IdorTests.cs
│   ├── Rbac/TicketForgeryTests.cs
│   ├── Rbac/PolicyTamperingTests.cs
│   ├── Api/LocalApiHardeningTests.cs
│   ├── Http/SsrfEgressTests.cs
│   ├── Secrets/LogRedactionTests.cs
│   ├── Audit/AuditTamperTests.cs
│   └── Input/InjectionAndFuzzTests.cs
│
├── Pipeline.Architecture.Tests/
│   ├── LayerDependencyTests.cs
│   ├── ProviderIsolationTests.cs
│   ├── EveryUseCaseHasPermissionTests.cs
│   └── NoDirectCredentialAccessTests.cs
│
└── Pipeline.Regression.Tests/
    ├── Bugs/                              (REG_<IssueId>_*.cs)
    ├── Snapshots/PermissionMatrix.verified.txt
    └── Snapshots/StatusCodeMap.verified.txt
```

### 9.2 TestKit

#### `tests/Pipeline.TestKit/Builders/PrincipalBuilder.cs`
```csharp
namespace Pipeline.TestKit.Builders;

// GUIDE: Fluent builder: PrincipalBuilder.For("alice").WithRoles("Operator").WithScope(...).StepUpAt(clock.Now).Build()
// GUIDE: Defaults must be the LEAST privileged valid principal (Viewer, empty scope).
public sealed class PrincipalBuilder { /* TODO */ }
```

#### `tests/Pipeline.TestKit/Builders/PolicyBundleFactory.cs`
```csharp
namespace Pipeline.TestKit.Builders;

// GUIDE: Creates bundles signed with a TEST key pair (generated in-memory per test run).
// GUIDE: Provide helpers: Valid(), Expired(), Tampered(), RolledBack(version), UnknownRole(), SoDViolation().
public static class PolicyBundleFactory { /* TODO */ }
```

#### `tests/Pipeline.TestKit/Fakes/FakeClock.cs`
```csharp
namespace Pipeline.TestKit.Fakes;

// GUIDE: Implements IClock. Methods: Advance(TimeSpan), Set(DateTimeOffset). Used for expiry, TTL, step-up tests.
public sealed class FakeClock : IClock { /* TODO */ }
```

### 9.3 Domain.Tests (unit and state-machine paths)

#### `tests/Pipeline.Domain.Tests/Jobs/JobStateMachinePathTests.cs`
```csharp
namespace Pipeline.Domain.Tests.Jobs;

[Trait("Category", "Path")]
public class JobStateMachinePathTests
{
    // GUIDE: Build the FULL N x N transition table from the state diagram
    //        (Pending, Validating, Running, Failed, Succeeded, RetryScheduled, NeedsReauthorization, Cancelled).
    // GUIDE: One Theory iterates every (from, to) pair; expected = legal set. ALL illegal pairs must throw/return Failure.
    [Theory(Skip = "TODO")]
    public void Transition_AllPairs_MatchLegalTable(JobStatus from, JobStatus to, bool expectedLegal) { }

    // GUIDE: Terminal states (Succeeded, Cancelled) have NO outgoing transitions.
    [Fact(Skip = "TODO")] public void Terminal_States_CannotTransition() { }

    // GUIDE: Walk every simple path from Pending to a terminal state and assert each hop is legal.
    [Fact(Skip = "TODO")] public void AllSimplePaths_PendingToTerminal_AreLegal() { }
}
```

#### `tests/Pipeline.Domain.Tests/Security/SeparationOfDutiesRuleTests.cs`
```csharp
namespace Pipeline.Domain.Tests.Security;

[Trait("Category", "Unit")]
public class SeparationOfDutiesRuleTests
{
    [Fact(Skip = "TODO")] public void Approver_SameAsRequester_NotAllowed() { }
    [Fact(Skip = "TODO")] public void Approver_DifferentUser_Allowed() { }
    [Fact(Skip = "TODO")] public void Auditor_WithWritePermission_InvalidRoleSet() { }
    // GUIDE: Add FsCheck property: for any two distinct ids, IsApproverAllowed(a,b) is true; for (a,a) always false.
}
```

#### `tests/Pipeline.Domain.Tests/Security/RoleCatalogTests.cs`
```csharp
namespace Pipeline.Domain.Tests.Security;

[Trait("Category", "Unit")]
public class RoleCatalogTests
{
    // GUIDE: Every permission referenced by any role exists in PermissionCatalog.
    [Fact(Skip = "TODO")] public void AllRolePermissions_ExistInCatalog() { }
    // GUIDE: No role contains BOTH payments.checkout.create and payments.checkout.approve.
    [Fact(Skip = "TODO")] public void NoRole_HoldsCreateAndApprovePayments() { }
    // GUIDE: Every T3 permission is held by at most the roles you intend (explicit allow-list in the test).
    [Fact(Skip = "TODO")] public void T3Permissions_AreHeldOnlyByIntendedRoles() { }
}
```

### 9.4 Application.Tests (authorization matrix, paths)

#### `tests/Pipeline.Application.Tests/Authorization/AuthorizationMatrixTests.cs`
```csharp
namespace Pipeline.Application.Tests.Authorization;

[Trait("Category", "Unit")]
public class AuthorizationMatrixTests
{
    // GUIDE: Data source = ALL roles x ALL permissions from the catalogs (generated, not hand-written).
    //        Expected value comes from the Section 2.4 matrix kept as an independent test fixture (CSV/JSON),
    //        so a code change cannot silently update its own expectation.
    // GUIDE: In-scope resource, fresh step-up, valid approval are all given, so ONLY the permission varies.
    [Theory(Skip = "TODO")]
    [MemberData(nameof(AllRolePermissionPairs))]
    public Task Authorize_RolePermissionPair_MatchesMatrix(string role, string permission, bool expectedAllow)
        => Task.CompletedTask;

    public static IEnumerable<object[]> AllRolePermissionPairs() => throw new NotImplementedException();
}
```

#### `tests/Pipeline.Application.Tests/Authorization/AuthorizationDecisionPathTests.cs`
```csharp
namespace Pipeline.Application.Tests.Authorization;

[Trait("Category", "Path")]
public class AuthorizationDecisionPathTests
{
    // GUIDE: One test per path in Section 10.1. Assert BOTH decision.ReasonCode AND decision.PathId.
    [Fact(Skip = "TODO")] public Task AUTHZ_P01_NoPrincipal_Denies() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P02_ExpiredSession_Denies() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P03_InvalidOrRolledBackPolicy_Denies() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P04_UnmappedOperation_Denies() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P05_PermissionMissing_Denies() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P06_ResourceOutOfScope_Denies() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P07_StaleStepUp_Challenges() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P08a_NoApproval_PendingApproval() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P08b_SelfApproval_Denies() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P09_RateLimited_Denies() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P10_AllChecksPass_IssuesTicket() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_P11_AnyOutcome_WritesAuditRecord() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task AUTHZ_PX_UnexpectedException_FailsClosed() => Task.CompletedTask;
}
```

#### `tests/Pipeline.Application.Tests/Authorization/AuthorizationTicketTests.cs`
```csharp
namespace Pipeline.Application.Tests.Authorization;

[Trait("Category", "Unit")]
public class AuthorizationTicketTests
{
    [Fact(Skip = "TODO")] public void Ticket_IsBoundTo_OperationFingerprint() { }
    [Fact(Skip = "TODO")] public void Ticket_ExpiresAfterTtl_PerTier() { }     // use FakeClock
    [Fact(Skip = "TODO")] public void Ticket_SecondUse_IsRejected() { }
    [Fact(Skip = "TODO")] public void Ticket_ModifiedField_FailsMac() { }
    [Fact(Skip = "TODO")] public void Ticket_WrongProvider_IsRejected() { }
}
```

#### `tests/Pipeline.Application.Tests/Pipeline/DispatcherOrderTests.cs`
```csharp
namespace Pipeline.Application.Tests.Pipeline;

[Trait("Category", "Unit")]
public class DispatcherOrderTests
{
    // GUIDE: Use recording fakes for each behavior; assert call order is exactly
    //        Authentication, Validation, Authorization, RateLimit, Idempotency, Audit, Handler.
    [Fact(Skip = "TODO")] public void Behaviors_RunInFixedOrder() { }
    // GUIDE: If Authorization denies, handler and connector are NEVER invoked (verify with Received(0)).
    [Fact(Skip = "TODO")] public void DeniedRequest_NeverReachesHandler() { }
    [Fact(Skip = "TODO")] public void HandlerException_IsMappedToStableError_AndAudited() { }
}
```

#### `tests/Pipeline.Application.Tests/Pipeline/IdempotencyBehaviorTests.cs`
```csharp
namespace Pipeline.Application.Tests.Pipeline;

[Trait("Category", "Path")]
public class IdempotencyBehaviorTests
{
    // GUIDE: Paths from Section 10.2 (IDEM_P01..P05).
    [Fact(Skip = "TODO")] public void IDEM_P01_NewKey_ExecutesAndStores() { }
    [Fact(Skip = "TODO")] public void IDEM_P02_SameKeySameFingerprintCompleted_ReturnsStoredResult() { }
    [Fact(Skip = "TODO")] public void IDEM_P03_SameKeySameFingerprintRunning_ReturnsInProgress() { }
    [Fact(Skip = "TODO")] public void IDEM_P04_SameKeyDifferentFingerprint_Returns409() { }
    [Fact(Skip = "TODO")] public void IDEM_P05_AmbiguousTimeout_MarksUnknown_NoBlindRetry() { }
}
```

#### `tests/Pipeline.Application.Tests/Pipeline/UseCaseRegistryValidatorTests.cs`
```csharp
namespace Pipeline.Application.Tests.Pipeline;

[Trait("Category", "Unit")]
public class UseCaseRegistryValidatorTests
{
    [Fact(Skip = "TODO")] public void HandlerWithoutAttribute_Throws() { }
    [Fact(Skip = "TODO")] public void HandlerWithUnknownPermission_Throws() { }
    [Fact(Skip = "TODO")] public void AllHandlersProperlyDeclared_DoesNotThrow() { }
}
```

### 9.5 Integration.Tests

#### `tests/Pipeline.Integration.Tests/Persistence/SqliteJobStoreTests.cs`
```csharp
namespace Pipeline.Integration.Tests.Persistence;

[Trait("Category", "Integration")]
public class SqliteJobStoreTests
{
    // GUIDE: Use a temp-file SQLite DB per test class; run real migrations.
    [Fact(Skip = "TODO")] public void IdempotencyKey_UniqueConstraint_Enforced() { }
    [Fact(Skip = "TODO")] public void ConcurrentSubmissions_SameKey_OnlyOneRows() { }  // Task.WhenAll
    [Fact(Skip = "TODO")] public void StateTransitions_ArePersistedAndOrdered() { }
    [Fact(Skip = "TODO")] public void RecoveryAfterCrash_RunningJobs_BecomeRetryScheduledOrUnknown() { }
}
```

#### `tests/Pipeline.Integration.Tests/Providers/Sheets/SheetsWireMockTests.cs`
```csharp
namespace Pipeline.Integration.Tests.Providers.Sheets;

[Trait("Category", "Integration")]
public class SheetsWireMockTests
{
    // GUIDE: Stub provider with WireMock; point the connector's base address at the stub.
    // GUIDE: Cover: 200, 400, 401 (=> NeedsReauthorization), 403, 404, 429 w/ Retry-After, 500, timeout, malformed JSON.
    [Fact(Skip = "TODO")] public void ReadRange_Success_MapsToContract() { }
    [Fact(Skip = "TODO")] public void Status401_MapsToNeedsReauthorization() { }
    [Fact(Skip = "TODO")] public void Status429_HonorsRetryAfter_BoundedRetries() { }
    [Fact(Skip = "TODO")] public void Timeout_OnWrite_DoesNotBlindRetry() { }
    [Fact(Skip = "TODO")] public void MalformedBody_MapsToStableError_NoRawBodyLeak() { }
}
```

> Repeat the same shape for `Gmail`, `Chat`, `ZohoExpense`, `Bigin`, `PayMongo`. Sandbox variants (`*SandboxTests.cs`) use the real provider test account, carry `[Trait("Category","Sandbox")]`, and read credentials from environment variables, **never from the repo**.

### 9.6 Contract.Tests

#### `tests/Pipeline.Contract.Tests/Providers/SheetsRequestContractTests.cs`
```csharp
namespace Pipeline.Contract.Tests.Providers;

[Trait("Category", "Contract")]
public class SheetsRequestContractTests
{
    // GUIDE: Capture the outgoing HttpRequestMessage via a recording handler; snapshot (Verify):
    //        method, path, query, content-type, body, and the ABSENCE of caller-supplied headers.
    // GUIDE: A diff in the snapshot = deliberate contract change; require review.
    [Fact(Skip = "TODO")] public Task UpdateRange_Request_MatchesSnapshot() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task ReadRange_Request_UsesGetAndNoBody() => Task.CompletedTask;
}
```

#### `tests/Pipeline.Contract.Tests/Api/ErrorContractTests.cs`
```csharp
namespace Pipeline.Contract.Tests.Api;

[Trait("Category", "Contract")]
public class ErrorContractTests
{
    // GUIDE: Every error is ProblemDetails with an app-specific "code" extension.
    // GUIDE: Assert no field contains: "Bearer ", "access_token", "StackTrace", "at Pipeline." (stack frames).
    [Fact(Skip = "TODO")] public void AllErrorCodes_AreStableAndDocumented() { }
    [Fact(Skip = "TODO")] public void ErrorBodies_NeverContainSecretsOrStackTraces() { }
}
```

### 9.7 Api.Tests

#### `tests/Pipeline.Api.Tests/Endpoints/StatusCodeMatrixTests.cs`
```csharp
namespace Pipeline.Api.Tests.Endpoints;

[Trait("Category", "Api")]
public class StatusCodeMatrixTests
{
    // GUIDE: Use WebApplicationFactory<Program> with fakes for connectors and a TestKit Principal.
    // GUIDE: Table-drive: (method, route, principal/token state, body) => expected HTTP status.
    [Theory(Skip = "TODO")]
    [InlineData("POST", "/api/v1/jobs", "valid-token+operator+valid-body", 202)]
    [InlineData("POST", "/api/v1/jobs", "no-token", 401)]
    [InlineData("POST", "/api/v1/jobs", "valid-token+viewer", 403)]
    [InlineData("POST", "/api/v1/jobs", "valid-token+operator+invalid-body", 400)]
    [InlineData("POST", "/api/v1/jobs", "valid-token+operator+business-rule-violation", 422)]
    [InlineData("POST", "/api/v1/jobs", "same-idempotency-key-different-body", 409)]
    [InlineData("GET",  "/api/v1/jobs/{unknown}", "valid-token+operator", 404)]
    [InlineData("POST", "/api/v1/jobs", "rate-limit-exceeded", 429)]
    public Task Endpoint_ReturnsExpectedStatus(string method, string route, string scenario, int expected)
        => Task.CompletedTask;
}
```

#### `tests/Pipeline.Api.Tests/Endpoints/JobsEndpointTests.cs`
```csharp
namespace Pipeline.Api.Tests.Endpoints;

[Trait("Category", "Api")]
public class JobsEndpointTests
{
    [Fact(Skip = "TODO")] public Task CreateJob_Returns202_WithLocationHeader() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task GetJob_ReturnsSafeSummary_NoSecrets() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task CancelJob_RunningJob_Returns202() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task GET_Endpoints_NeverCauseSideEffects() => Task.CompletedTask; // HTTP semantics guard
}
```

### 9.8 Security.Tests

#### `tests/Pipeline.Security.Tests/Rbac/PrivilegeEscalationTests.cs`
```csharp
namespace Pipeline.Security.Tests.Rbac;

[Trait("Category", "Security")]
public class PrivilegeEscalationTests
{
    // GUIDE: Drive through IUseCaseDispatcher (the real pipeline), NOT through AuthorizationService alone.
    [Fact(Skip = "TODO")] public Task SEC_RBAC_ViewerCallsSheetsWrite_Denied() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_RBAC_OperatorCallsPaymentsCreate_Denied() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_RBAC_FinanceCreatesAndApprovesOwnPayment_Denied() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_RBAC_OwnerChangesOwnRoles_Denied() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_RBAC_RoleNameInRequestBody_IsIgnored() => Task.CompletedTask; // mass-assignment
    [Fact(Skip = "TODO")] public Task SEC_RBAC_ApprovalReusedForDifferentAmount_Denied() => Task.CompletedTask;
}
```

#### `tests/Pipeline.Security.Tests/Rbac/IdorTests.cs`
```csharp
namespace Pipeline.Security.Tests.Rbac;

[Trait("Category", "Security")]
public class IdorTests
{
    [Fact(Skip = "TODO")] public Task SEC_IDOR_SpreadsheetNotInScope_Denied() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_IDOR_ChatSpaceNotInScope_Denied() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_IDOR_JobOwnedByAnotherUser_NotFoundNotForbidden() => Task.CompletedTask; // no existence oracle
    [Fact(Skip = "TODO")] public Task SEC_IDOR_IdWithTrailingSpaceOrCaseChange_DoesNotBypassScope() => Task.CompletedTask;
}
```

#### `tests/Pipeline.Security.Tests/Rbac/TicketForgeryTests.cs`
```csharp
namespace Pipeline.Security.Tests.Rbac;

[Trait("Category", "Security")]
public class TicketForgeryTests
{
    // GUIDE: Use reflection ONLY to simulate an attacker; assert broker still refuses.
    [Fact(Skip = "TODO")] public Task SEC_TICKET_BrokerCalledWithoutTicket_NotPossible_ByCompileOrRefusedAtRuntime() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_TICKET_ReplayedTicket_Refused() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_TICKET_TicketFromOtherOperation_Refused() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_TICKET_ExpiredTicket_Refused() => Task.CompletedTask;
}
```

#### `tests/Pipeline.Security.Tests/Rbac/PolicyTamperingTests.cs`
```csharp
namespace Pipeline.Security.Tests.Rbac;

[Trait("Category", "Security")]
public class PolicyTamperingTests
{
    [Fact(Skip = "TODO")] public void SEC_POLICY_TamperedBundle_Rejected() { }
    [Fact(Skip = "TODO")] public void SEC_POLICY_ExpiredBundle_FailsClosed() { }
    [Fact(Skip = "TODO")] public void SEC_POLICY_OlderVersion_RollbackRejected() { }
    [Fact(Skip = "TODO")] public void SEC_POLICY_UnknownRoleOrPermission_Rejected() { }
    [Fact(Skip = "TODO")] public void SEC_POLICY_ClockRolledBack_Detected() { }
}
```

#### `tests/Pipeline.Security.Tests/Api/LocalApiHardeningTests.cs`
```csharp
namespace Pipeline.Security.Tests.Api;

[Trait("Category", "Security")]
public class LocalApiHardeningTests
{
    [Fact(Skip = "TODO")] public Task SEC_API_NoLaunchToken_401() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_API_WrongLaunchToken_401() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_API_NonLoopbackRemote_403() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_API_ForeignHostHeader_400() => Task.CompletedTask;       // DNS rebinding
    [Fact(Skip = "TODO")] public Task SEC_API_CorsPreflight_NoAllowOriginHeader() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_API_OversizedBody_413() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_API_ValidTokenButNoPermission_StillDenied() => Task.CompletedTask; // token != authz
    [Fact(Skip = "TODO")] public Task SEC_API_NoGenericProxyOrExecuteEndpointExists() => Task.CompletedTask;
}
```

#### `tests/Pipeline.Security.Tests/Http/SsrfEgressTests.cs`
```csharp
namespace Pipeline.Security.Tests.Http;

[Trait("Category", "Security")]
public class SsrfEgressTests
{
    // GUIDE: Drive EgressAllowlistHandler with a stub inner handler; assert the inner handler is NOT called.
    [Theory(Skip = "TODO")]
    [InlineData("http://sheets.googleapis.com/")]          // not HTTPS
    [InlineData("https://evil.example.com/")]              // host not allowed
    [InlineData("https://127.0.0.1/")]                     // IP literal / loopback
    [InlineData("https://169.254.169.254/")]               // metadata address
    [InlineData("https://sheets.googleapis.com@evil.com/")]// userinfo trick
    [InlineData("https://sheets.googleapis.com.evil.com/")]// suffix trick
    public Task Egress_Blocks(string url) => Task.CompletedTask;

    [Fact(Skip = "TODO")] public Task Redirect_ToDisallowedHost_Blocked() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task CallerSuppliedAuthorizationHeader_IsStripped() => Task.CompletedTask;
}
```

#### `tests/Pipeline.Security.Tests/Secrets/LogRedactionTests.cs`
```csharp
namespace Pipeline.Security.Tests.Secrets;

[Trait("Category", "Security")]
public class LogRedactionTests
{
    [Fact(Skip = "TODO")] public void Logs_NeverContain_BearerTokens() { }
    [Fact(Skip = "TODO")] public void AccessToken_ToString_IsRedacted() { }
    [Fact(Skip = "TODO")] public void ExceptionMessages_AreRedactedBeforeLogging() { }
    [Fact(Skip = "TODO")] public void ApiResponses_NeverContainRefreshTokens() { }
    [Fact(Skip = "TODO")] public void PublishedBinaryAndConfig_ContainNoSecrets() { } // grep for known test-secret markers
}
```

#### `tests/Pipeline.Security.Tests/Audit/AuditTamperTests.cs`
```csharp
namespace Pipeline.Security.Tests.Audit;

[Trait("Category", "Security")]
public class AuditTamperTests
{
    [Fact(Skip = "TODO")] public Task SEC_AUDIT_EditedRow_ChainBreaksAtThatSequence() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_AUDIT_DeletedRow_ChainBreaks() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_AUDIT_ReorderedRows_ChainBreaks() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task SEC_AUDIT_BrokenChainAtStartup_BlocksLaunchOrAlerts() => Task.CompletedTask;
}
```

#### `tests/Pipeline.Security.Tests/Input/InjectionAndFuzzTests.cs`
```csharp
namespace Pipeline.Security.Tests.Input;

[Trait("Category", "Security")]
public class InjectionAndFuzzTests
{
    // GUIDE: Reuse one abuse-string corpus (SQLi, header injection CRLF, path traversal, oversized, unicode
    //        confusables, null bytes, formula injection "=cmd|..." for Sheets cells).
    [Theory(Skip = "TODO")] public Task SqlMetaCharacters_DoNotAlterQueries(string payload) => Task.CompletedTask;
    [Theory(Skip = "TODO")] public Task CrlfInEmailFields_IsRejected(string payload) => Task.CompletedTask;
    [Theory(Skip = "TODO")] public Task SheetCellFormulaPrefix_IsNeutralizedOrExplicitlyAllowed(string payload) => Task.CompletedTask;
    [Fact(Skip = "TODO")]  public void IpcUnknownCommand_IsRejected() { }
    [Fact(Skip = "TODO")]  public void IpcMalformedJson_ReturnsStableError_NoCrash() { }
}
```

### 9.9 Architecture.Tests (structural bypass prevention)

#### `tests/Pipeline.Architecture.Tests/LayerDependencyTests.cs`
```csharp
namespace Pipeline.Architecture.Tests;

[Trait("Category", "Architecture")]
public class LayerDependencyTests
{
    // GUIDE: Use NetArchTest.Rules. Assert (assembly-level):
    [Fact(Skip = "TODO")] public void Domain_DependsOnNothingExternal() { }                 // no Application/Infrastructure/HTTP/SDKs
    [Fact(Skip = "TODO")] public void Application_DoesNotDependOn_Infrastructure_Or_Providers() { }
    [Fact(Skip = "TODO")] public void Presentation_And_Host_DoNotReference_ProviderModules() { }
    [Fact(Skip = "TODO")] public void NoCircularProjectReferences() { }
}
```

#### `tests/Pipeline.Architecture.Tests/ProviderIsolationTests.cs`
```csharp
namespace Pipeline.Architecture.Tests;

[Trait("Category", "Architecture")]
public class ProviderIsolationTests
{
    [Fact(Skip = "TODO")] public void ProviderModules_DoNotReference_EachOther() { }
    [Fact(Skip = "TODO")] public void ConnectorClasses_AreInternal() { }
    [Fact(Skip = "TODO")] public void ProviderDtos_DoNotLeak_IntoContractsOrPresentation() { }
}
```

#### `tests/Pipeline.Architecture.Tests/EveryUseCaseHasPermissionTests.cs`
```csharp
namespace Pipeline.Architecture.Tests;

[Trait("Category", "Architecture")]
public class EveryUseCaseHasPermissionTests
{
    [Fact(Skip = "TODO")] public void EveryHandler_HasRequiresPermission_WithCatalogPermission() { }
    [Fact(Skip = "TODO")] public void EveryPermissionInCatalog_IsUsedByAtLeastOneHandler_OrExplicitlyReserved() { }
}
```

#### `tests/Pipeline.Architecture.Tests/NoDirectCredentialAccessTests.cs`
```csharp
namespace Pipeline.Architecture.Tests;

[Trait("Category", "Architecture")]
public class NoDirectCredentialAccessTests
{
    [Fact(Skip = "TODO")] public void ICredentialStore_ImplementationsAreInternal_AndOnlyUsedByCredentialBroker() { }
    [Fact(Skip = "TODO")] public void OnlyAuthorizationService_CanConstruct_AuthorizationTicket() { }
    [Fact(Skip = "TODO")] public void NoType_OutsideInfrastructureSecurity_UsesProtectedData() { }
    [Fact(Skip = "TODO")] public void NoHttpClient_IsCreatedWithNew_OutsideFactory() { }
}
```

### 9.10 Regression.Tests

#### `tests/Pipeline.Regression.Tests/Snapshots/PermissionMatrixSnapshotTests.cs`
```csharp
namespace Pipeline.Regression.Tests.Snapshots;

[Trait("Category", "Regression")]
public class PermissionMatrixSnapshotTests
{
    // GUIDE: Render the FULL role x permission matrix from RoleCatalog to deterministic text and
    //        verify against PermissionMatrix.verified.txt (Verify library).
    // GUIDE: Any permission change shows up as a snapshot diff. Protect the snapshot with CODEOWNERS
    //        (security reviewer must approve).
    [Fact(Skip = "TODO")] public Task RoleCatalog_Matrix_MatchesApprovedSnapshot() => Task.CompletedTask;
    [Fact(Skip = "TODO")] public Task ErrorCodeToHttpStatus_Map_MatchesApprovedSnapshot() => Task.CompletedTask;
}
```

#### `tests/Pipeline.Regression.Tests/Bugs/REG_0000_Template.cs`
```csharp
namespace Pipeline.Regression.Tests.Bugs;

// GUIDE: Copy this file for EVERY fixed bug. Rules:
//   1) Write the failing test FIRST (reproduces the bug), then fix, then keep the test forever.
//   2) File name and test name carry the issue ID.
//   3) Include a one-line comment with root cause. Do not delete regression tests to "clean up".
[Trait("Category", "Regression")]
public class REG_0000_Template
{
    [Fact(Skip = "TODO")] public void REG_0000_ShortDescription() { }
}
```

### 9.11 Frontend tests (Jest + React Testing Library)

```
src/Pipeline.Presentation/__tests__/
├── screens/        // render states: loading, success, error, needs-reauth, access-denied
├── services/       // IPC client: only sends allow-listed commands; never stores tokens
└── contracts/      // TypeScript types stay in sync with Pipeline.Contracts (generated or snapshot-checked)
```

Guidance:
- UI tests verify **presentation only** ("show Access Denied when backend returns 403"). They are not security tests.
- Assert the IPC client has no code path that accepts a URL, HTTP method, or header from components.
- Assert no token or secret is written to `localStorage`, `sessionStorage`, or React state.

---

## 10. Path testing

Path testing means every **independent decision path** through security- and money-critical logic has at least one test. For each target method, the number of required independent paths equals its cyclomatic complexity (decision points + 1). Track path IDs in test names and in `AuthorizationDecision.PathId`.

### 10.1 Authorization paths

| Path ID | Step | Trigger | Expected outcome | Reason code |
|---|---|---|---|---|
| AUTHZ_P01 | S01 | No principal | Deny | `AUTHN_MISSING` |
| AUTHZ_P02 | S02 | Session expired/revoked | Deny | `SESSION_INVALID` |
| AUTHZ_P03 | S03 | Bundle invalid, expired, or rolled back | Deny | `POLICY_INVALID` |
| AUTHZ_P04 | S04 | Operation has no mapped permission | Deny | `OPERATION_UNMAPPED` |
| AUTHZ_P05 | S05 | Principal lacks permission | Deny | `PERMISSION_MISSING` |
| AUTHZ_P06 | S06 | Resource outside scope | Deny | `RESOURCE_OUT_OF_SCOPE` |
| AUTHZ_P07 | S07 | T2+ and stale step-up | Challenge | `STEPUP_REQUIRED` |
| AUTHZ_P08a | S08 | T3 and no approval | PendingApproval | `APPROVAL_REQUIRED` |
| AUTHZ_P08b | S08 | T3 and approver == requester | Deny | `SOD_VIOLATION` |
| AUTHZ_P09 | S09 | Rate/concurrency exceeded | Deny | `RATE_LIMITED` |
| AUTHZ_P10 | S10 | All checks pass | Allow + Ticket | `OK` |
| AUTHZ_P11 | S11 | Any outcome | Audit written | n/a |
| AUTHZ_PX | n/a | Unexpected exception | Deny (fail closed) | `INTERNAL_DENY` |

**Ordering test:** also assert that when **two** checks would fail (e.g., no permission *and* out of scope), the **earlier** step's reason is returned. This catches accidental reordering.

### 10.2 Idempotency paths

| Path ID | Situation | Expected |
|---|---|---|
| IDEM_P01 | New key | Execute, persist result |
| IDEM_P02 | Same key, same fingerprint, completed | Return stored result, no re-execution |
| IDEM_P03 | Same key, same fingerprint, still running | Return "in progress" |
| IDEM_P04 | Same key, different fingerprint | `409 Conflict` |
| IDEM_P05 | Provider call timed out (ambiguous) | Mark `Unknown`; require reconciliation, **no blind retry** |

### 10.3 Other path targets

| Target | Paths to enumerate |
|---|---|
| `JobStateMachine.Transition` | Full N×N matrix (legal and illegal), terminal states, retry loop bounds |
| `CredentialBroker.GetAccessTokenAsync` | MAC bad / expired / nonce used / provider mismatch / account mismatch / refresh needed / refresh failed / success |
| `SignedPolicyBundleVerifier` | Signature bad / expired / rollback / unknown role / SoD violation / valid |
| `EgressAllowlistHandler` | Scheme / host / IP literal / userinfo / redirect / valid |
| Provider error translator | One path per documented status class: 2xx, 4xx variants, 401, 429, 5xx, timeout, malformed body |
| Token refresh | Refresh success / invalid_grant (=> `NeedsReauthorization`) / network failure / concurrent refresh |

### 10.4 Coverage targets for path-critical code

| Metric | Target |
|---|---|
| Branch coverage on `Pipeline.Domain.Security`, `Application.Authorization`, `Infrastructure.Security`, `EgressAllowlistHandler` | **≥ 95%** |
| Mutation score on the same namespaces (Stryker.NET) | **≥ 85%** |
| Every `AUTHZ_P*` / `IDEM_P*` path | **100%** (CI check that each path ID appears in at least one passing test) |

---

## 11. Security test catalog

| ID | Area | Attack / abuse case | Expected result | Test file |
|---|---|---|---|---|
| SEC_RBAC_01 | RBAC | Viewer invokes write operation | 403, no connector call | `PrivilegeEscalationTests` |
| SEC_RBAC_02 | RBAC | Operator invokes payment creation | 403 | `PrivilegeEscalationTests` |
| SEC_RBAC_03 | RBAC | Requester approves own payment | Denied (`SOD_VIOLATION`) | `PrivilegeEscalationTests` |
| SEC_RBAC_04 | RBAC | Role name supplied in request body | Ignored | `PrivilegeEscalationTests` |
| SEC_RBAC_05 | RBAC | Approval reused with altered amount | Denied (fingerprint mismatch) | `PrivilegeEscalationTests` |
| SEC_IDOR_01 | RBAC | Spreadsheet/space ID outside scope | Denied | `IdorTests` |
| SEC_IDOR_02 | RBAC | Another user's job ID | 404 (no existence oracle) | `IdorTests` |
| SEC_TICKET_01 | Capability | Replay ticket | Refused | `TicketForgeryTests` |
| SEC_TICKET_02 | Capability | Ticket from different operation | Refused | `TicketForgeryTests` |
| SEC_POLICY_01 | Policy | Tampered bundle | Rejected, fail closed | `PolicyTamperingTests` |
| SEC_POLICY_02 | Policy | Older bundle version | Rejected | `PolicyTamperingTests` |
| SEC_API_01 | Local API | No/wrong launch token | 401 | `LocalApiHardeningTests` |
| SEC_API_02 | Local API | Foreign `Host` header | 400 | `LocalApiHardeningTests` |
| SEC_API_03 | Local API | Valid token, no permission | Still denied | `LocalApiHardeningTests` |
| SEC_API_04 | Local API | Search for generic proxy/execute route | None exists | `LocalApiHardeningTests` |
| SEC_SSRF_01 | Egress | Non-allowlisted host, IP literal, metadata IP | Blocked before send | `SsrfEgressTests` |
| SEC_SSRF_02 | Egress | Redirect to other host | Blocked | `SsrfEgressTests` |
| SEC_SEC_01 | Secrets | Token appears in logs/errors/responses | Never | `LogRedactionTests` |
| SEC_AUDIT_01 | Audit | Edit, delete, or reorder rows | Chain break detected | `AuditTamperTests` |
| SEC_INPUT_01 | Input | SQLi, CRLF, traversal, formula injection | Neutralized or rejected | `InjectionAndFuzzTests` |
| SEC_IPC_01 | IPC | Unknown command or malformed JSON | Stable error, no crash | `InjectionAndFuzzTests` |
| SEC_ARCH_01 | Structure | Handler without permission attribute | Build/startup fails | `EveryUseCaseHasPermissionTests` |
| SEC_ARCH_02 | Structure | Direct credential store access | Build fails | `NoDirectCredentialAccessTests` |

**Manual / periodic security work (not automatable in unit tests)**

- Threat-model review at each new provider or permission.
- Dependency and SBOM review before each release.
- Penetration test of the packaged build on a clean Windows VM (IPC, localhost port if any, file ACLs, WebView2 hardening).
- Review of the policy-bundle signing key custody and rotation process.

---

## 12. Regression testing

| Mechanism | What it protects | How |
|---|---|---|
| **Bug-fix tests** | Previously fixed defects | `REG_<IssueId>_*` created *before* the fix; never deleted |
| **Permission matrix snapshot** | Accidental privilege changes | Verify snapshot of the role × permission matrix; security-owner approval required |
| **Contract snapshots** | Provider request shapes, API error shapes, OpenAPI | Verify snapshots; diffs reviewed |
| **Status code map snapshot** | Error code to HTTP status mapping | Verify snapshot |
| **State machine table** | Illegal job transitions | Full N×N Theory |
| **Golden workflow runs** | End-to-end pipeline behavior with stubbed providers | WireMock scripts + expected `JobStatus` history |
| **Mutation testing** | Tests that pass but assert nothing | Stryker.NET nightly on security namespaces |

**Regression workflow**

1. Bug reported, write a failing `REG_` test that reproduces it.
2. Fix the code, the test passes.
3. Add a security tag if the bug had any security impact (`Category=Security` in addition to `Regression` via a second trait, e.g., `[Trait("Area","Security")]`).
4. Never weaken or delete a regression test without written justification in the PR.

---

## 13. CI quality gates

| Gate | Command / tool | Fails the build when |
|---|---|---|
| Build, warnings as errors | `dotnet build -warnaserror` | Any warning |
| Unit + path + architecture | `dotnet test --filter "Category=Unit\|Category=Path\|Category=Architecture"` | Any failure |
| API + integration + contract | `dotnet test --filter "Category=Api\|Category=Integration\|Category=Contract"` | Any failure or snapshot diff |
| Security + regression | `dotnet test --filter "Category=Security\|Category=Regression"` | Any failure |
| Coverage | Coverlet + threshold | Branch coverage < 95% on security namespaces; < 80% elsewhere |
| Path-ID check | Script scanning test names | Any `AUTHZ_P*` / `IDEM_P*` missing |
| Mutation (nightly) | Stryker.NET | Score < 85% on security namespaces |
| Dependency audit | `dotnet list package --vulnerable --include-transitive` | Known vulnerabilities |
| Locked restore | `RestoreLockedMode=true` | Lock file drift |
| Static analysis | CodeQL or Roslyn analyzers (security rules) | New high-severity findings |
| Secret scan | gitleaks (or equivalent) | Any secret detected |
| SBOM | CycloneDX generator | SBOM not produced |
| Frontend | `jest --ci`, ESLint, `npm audit` | Failures or high-severity advisories |
| Release signing | Authenticode signing step | Unsigned artifact |
| Sandbox E2E (nightly/manual) | `dotnet test --filter "Category=Sandbox"` | Provider contract drift |

---

## 14. Implementation order

| Step | Deliverable | Done when |
|---|---|---|
| 1 | `Pipeline.Domain/Security` + `Domain.Tests` | Catalogs, roles, SoD rules compile; matrix and SoD tests pass |
| 2 | `IUseCaseDispatcher` + behaviors (stub handlers) | `DispatcherOrderTests` pass; denied requests never reach handlers |
| 3 | `AuthorizationService` + `AuthorizationTicket` | All `AUTHZ_P*` path tests pass; matrix test passes |
| 4 | `UseCaseRegistryValidator` + `Architecture.Tests` | Unprotected handler fails build/startup |
| 5 | `SignedPolicyBundleVerifier` + `SessionManager` | `PolicyTamperingTests` pass |
| 6 | `CredentialBroker` + `DpapiCredentialStore` | `TicketForgeryTests`, `NoDirectCredentialAccessTests` pass |
| 7 | `HashChainAuditWriter` | `AuditTamperTests` pass |
| 8 | `EgressAllowlistHandler` | `SsrfEgressTests` pass |
| 9 | IPC router + WebView hardening (and LocalApi security if used) | `LocalApiHardeningTests`, IPC fuzz tests pass |
| 10 | First provider (Sheets, read-only) end to end | WireMock + contract tests pass; sandbox test green |
| 11 | CI gates enabled | All gates in Section 13 active **before** adding more providers |
| 12 | Remaining providers one at a time | Each adds WireMock, contract, security cases (T2/T3 get step-up and approval tests) |
| 13 | PayMongo via trusted server only | Server-side RBAC mirrors client; webhook signature, replay, and duplicate tests pass |

---

### Final reminders

1. **RBAC is only as trustworthy as the source of roles.** Signed, server-issued policy is what turns RBAC from "advisory" into "tamper-evident."
2. **Structure beats discipline.** Capability tickets, `internal` visibility, startup validation, and architecture tests mean a new developer cannot accidentally open a bypass.
3. **A local admin can still win in-process.** The design raises cost, detects tampering, and keeps high-value secrets (PayMongo) off the desktop.
4. **Verify provider facts** (hosts, scopes, idempotency support, webhook signing) against each vendor's current official documentation before locking the allowlists and permission catalog.
