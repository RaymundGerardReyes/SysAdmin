using Pipeline.Application.Authorization;
using Pipeline.Contracts.Errors;
using Pipeline.Domain.Security;
using Pipeline.TestKit.Builders;
using Pipeline.TestKit.Fakes;
using Shouldly;
using Xunit;

namespace Pipeline.Application.Tests.Authorization;

[Trait("Category", "Path")]
public class AuthorizationDecisionPathTests
{
    private readonly FakeClock _clock = new();
    private readonly InMemoryPrincipalAccessor _principalAccessor = new();
    private readonly InMemoryPolicyBundleProvider _policyProvider = new();
    private readonly ScopedResourceAuthorizer _resourceAuthorizer = new();
    private readonly InMemoryStepUpAuthenticator _stepUpAuth = new();
    private readonly InMemoryApprovalService _approvalService = new();
    private readonly RecordingRateLimiter _rateLimiter = new();
    private readonly InMemoryAuditWriter _auditWriter = new();

    private AuthorizationService CreateService() =>
        new(
            _principalAccessor,
            _policyProvider,
            _resourceAuthorizer,
            _stepUpAuth,
            _approvalService,
            _rateLimiter,
            _clock,
            _auditWriter);

    [Fact]
    public async Task AUTHZ_P01_NoPrincipal_Denies()
    {
        _principalAccessor.Current = null;
        var service = CreateService();

        var request = new AuthorizationRequest("op", "jobs.read", "fp", null, null, new Dictionary<string, string>());
        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Deny);
        decision.ReasonCode.ShouldBe(ErrorCodes.AuthnMissing);
        decision.PathId.ShouldBe("AUTHZ_P01");
    }

    [Fact]
    public async Task AUTHZ_P02_ExpiredSession_Denies()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice")
            .ExpiresAt(_clock.UtcNow.AddMinutes(-1))
            .Build();
        var service = CreateService();

        var request = new AuthorizationRequest("op", "jobs.read", "fp", null, null, new Dictionary<string, string>());
        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Deny);
        decision.ReasonCode.ShouldBe(ErrorCodes.SessionInvalid);
        decision.PathId.ShouldBe("AUTHZ_P02");
    }

    [Fact]
    public async Task AUTHZ_P03_InvalidOrRolledBackPolicy_Denies()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice").Build();
        _policyProvider.Current = null; // No valid policy bundle
        var service = CreateService();

        var request = new AuthorizationRequest("op", "jobs.read", "fp", null, null, new Dictionary<string, string>());
        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Deny);
        decision.ReasonCode.ShouldBe(ErrorCodes.PolicyInvalid);
        decision.PathId.ShouldBe("AUTHZ_P03");
    }

    [Fact]
    public async Task AUTHZ_P04_UnmappedOperation_Denies()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice").Build();
        _policyProvider.Current = PolicyBundleFactory.CreateInMemoryBundle();
        var service = CreateService();

        var request = new AuthorizationRequest("op", "nonexistent.permission.format", "fp", null, null, new Dictionary<string, string>());
        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Deny);
        decision.ReasonCode.ShouldBe(ErrorCodes.OperationUnmapped);
        decision.PathId.ShouldBe("AUTHZ_P04");
    }

    [Fact]
    public async Task AUTHZ_P05_PermissionMissing_Denies()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice")
            .WithRoles(RoleCatalog.ViewerRole)
            .Build();
        _policyProvider.Current = PolicyBundleFactory.CreateInMemoryBundle();
        var service = CreateService();

        // Viewer does not have sheets.values.write
        var request = new AuthorizationRequest("op", "sheets.values.write", "fp", null, null, new Dictionary<string, string>());
        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Deny);
        decision.ReasonCode.ShouldBe(ErrorCodes.PermissionMissing);
        decision.PathId.ShouldBe("AUTHZ_P05");
    }

    [Fact]
    public async Task AUTHZ_P06_ResourceOutOfScope_Denies()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice")
            .WithRoles(RoleCatalog.OperatorRole)
            .WithSpreadsheet("allowed_sheet")
            .Build();
        _policyProvider.Current = PolicyBundleFactory.CreateInMemoryBundle();
        var service = CreateService();

        var request = new AuthorizationRequest(
            "op",
            "sheets.values.write",
            "fp",
            null,
            null,
            new Dictionary<string, string> { ["spreadsheetId"] = "foreign_sheet" });

        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Deny);
        decision.ReasonCode.ShouldBe(ErrorCodes.ResourceOutOfScope);
        decision.PathId.ShouldBe("AUTHZ_P06");
    }

    [Fact]
    public async Task AUTHZ_P07_StaleStepUp_Challenges()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice")
            .WithRoles(RoleCatalog.OperatorRole)
            .StepUpAt(null)
            .Build();
        _policyProvider.Current = PolicyBundleFactory.CreateInMemoryBundle();
        _stepUpAuth.AlwaysFresh = false; // Step up is required
        var service = CreateService();

        var request = new AuthorizationRequest("op", "gmail.messages.send", "fp", null, null, new Dictionary<string, string>());
        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Challenge);
        decision.ReasonCode.ShouldBe(ErrorCodes.StepupRequired);
        decision.PathId.ShouldBe("AUTHZ_P07");
    }

    [Fact]
    public async Task AUTHZ_P08a_NoApproval_PendingApproval()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice")
            .WithRoles(RoleCatalog.FinanceOperatorRole)
            .WithPaymentAccount("acc_1")
            .StepUpAt(_clock.UtcNow)
            .Build();
        _policyProvider.Current = PolicyBundleFactory.CreateInMemoryBundle();
        _stepUpAuth.AlwaysFresh = true;
        var service = CreateService();

        // T3 without approvalId
        var request = new AuthorizationRequest(
            "op",
            "payments.checkout.create",
            "fp",
            "PayMongo",
            "acc_1",
            new Dictionary<string, string> { ["paymentAccountId"] = "acc_1" },
            null);

        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.PendingApproval);
        decision.ReasonCode.ShouldBe(ErrorCodes.ApprovalRequired);
        decision.PathId.ShouldBe("AUTHZ_P08a");
    }

    [Fact]
    public async Task AUTHZ_P08b_SelfApproval_Denies()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice")
            .WithRoles(RoleCatalog.FinanceOperatorRole)
            .WithPaymentAccount("acc_1")
            .StepUpAt(_clock.UtcNow)
            .Build();
        _policyProvider.Current = PolicyBundleFactory.CreateInMemoryBundle();
        _stepUpAuth.AlwaysFresh = true;
        _approvalService.SeedApproval("appr_self", "fp", "alice"); // Self-requester
        var service = CreateService();

        var request = new AuthorizationRequest(
            "op",
            "payments.checkout.create",
            "fp",
            "PayMongo",
            "acc_1",
            new Dictionary<string, string> { ["paymentAccountId"] = "acc_1" },
            "appr_self");

        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Deny);
        decision.ReasonCode.ShouldBe(ErrorCodes.SodViolation);
        decision.PathId.ShouldBe("AUTHZ_P08b");
    }

    [Fact]
    public async Task AUTHZ_P09_RateLimited_Denies()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice")
            .WithRoles(RoleCatalog.OperatorRole)
            .Build();
        _policyProvider.Current = PolicyBundleFactory.CreateInMemoryBundle();
        _rateLimiter.AllowAll = false; // Trigger rate limit
        var service = CreateService();

        var request = new AuthorizationRequest("op", "jobs.read", "fp", null, null, new Dictionary<string, string>());
        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Deny);
        decision.ReasonCode.ShouldBe(ErrorCodes.RateLimited);
        decision.PathId.ShouldBe("AUTHZ_P09");
    }

    [Fact]
    public async Task AUTHZ_P10_AllChecksPass_IssuesTicket()
    {
        _principalAccessor.Current = PrincipalBuilder.For("alice")
            .WithRoles(RoleCatalog.OperatorRole)
            .Build();
        _policyProvider.Current = PolicyBundleFactory.CreateInMemoryBundle();
        var service = CreateService();

        var request = new AuthorizationRequest("op", "jobs.read", "fp_123", "GoogleSheets", "default", new Dictionary<string, string>());
        var decision = await service.AuthorizeAsync(request);

        decision.Outcome.ShouldBe(DecisionOutcome.Allow);
        decision.PathId.ShouldBe("AUTHZ_P10");
        decision.Ticket.ShouldNotBeNull();
        decision.Ticket.VerifyMac().ShouldBeTrue();
    }

    [Fact]
    public async Task AUTHZ_P11_AnyOutcome_WritesAuditRecord()
    {
        _principalAccessor.Current = null;
        var service = CreateService();

        var request = new AuthorizationRequest("op", "jobs.read", "fp", null, null, new Dictionary<string, string>());
        await service.AuthorizeAsync(request);

        _auditWriter.Entries.ShouldNotBeEmpty();
        _auditWriter.Entries.Last().Outcome.ShouldBe("Deny");
    }

    [Fact]
    public async Task OrderingTest_EarlierStepReasonReturned()
    {
        // Both permission missing (S05) AND resource out of scope (S06) would fail.
        // The service must return S05 PERMISSION_MISSING, proving the ordering S05 before S06.
        _principalAccessor.Current = PrincipalBuilder.For("alice")
            .WithRoles(RoleCatalog.ViewerRole)
            .WithSpreadsheet("allowed_sheet")
            .Build();
        _policyProvider.Current = PolicyBundleFactory.CreateInMemoryBundle();
        var service = CreateService();

        var request = new AuthorizationRequest(
            "op",
            "sheets.values.write", // Viewer lacks this (S05)
            "fp",
            null,
            null,
            new Dictionary<string, string> { ["spreadsheetId"] = "foreign_sheet" }); // Also out of scope (S06)

        var decision = await service.AuthorizeAsync(request);

        decision.ReasonCode.ShouldBe(ErrorCodes.PermissionMissing);
        decision.PathId.ShouldBe("AUTHZ_P05");
    }
}
