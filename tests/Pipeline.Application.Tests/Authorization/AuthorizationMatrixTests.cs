using System.Text.Json;
using Pipeline.Application.Authorization;
using Pipeline.Domain.Security;
using Pipeline.TestKit.Builders;
using Pipeline.TestKit.Fakes;
using Shouldly;
using Xunit;

namespace Pipeline.Application.Tests.Authorization;

[Trait("Category", "Unit")]
public class AuthorizationMatrixTests
{
    private static readonly Dictionary<string, HashSet<string>> ExpectedMatrix;

    static AuthorizationMatrixTests()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "role_permission_matrix.json");
        if (!File.Exists(jsonPath))
        {
            // Fallback to relative path if running from project root
            jsonPath = Path.Combine("Fixtures", "role_permission_matrix.json");
        }

        var json = File.ReadAllText(jsonPath);
        var parsed = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json)!;
        ExpectedMatrix = parsed.ToDictionary(k => k.Key, v => new HashSet<string>(v.Value, StringComparer.Ordinal));
    }

    public static IEnumerable<object[]> AllRolePermissionPairs()
    {
        foreach (var roleName in RoleCatalog.Baseline.Keys)
        {
            foreach (var permission in PermissionCatalog.All.Keys)
            {
                var expected = ExpectedMatrix[roleName].Contains(permission.Value);
                yield return new object[] { roleName, permission.Value, expected };
            }
        }
    }

    [Theory]
    [MemberData(nameof(AllRolePermissionPairs))]
    public async Task Authorize_RolePermissionPair_MatchesMatrix(string roleName, string permissionStr, bool expectedAllow)
    {
        var permission = new Permission(permissionStr);
        var clock = new FakeClock();
        var principalAccessor = new InMemoryPrincipalAccessor();
        var policyProvider = new InMemoryPolicyBundleProvider { Current = PolicyBundleFactory.CreateInMemoryBundle() };
        var resourceAuthorizer = new ScopedResourceAuthorizer();
        var stepUpAuth = new InMemoryStepUpAuthenticator { AlwaysFresh = true };
        var approvalService = new InMemoryApprovalService();
        var rateLimiter = new RecordingRateLimiter { AllowAll = true };
        var auditWriter = new InMemoryAuditWriter();

        var authzService = new AuthorizationService(
            principalAccessor,
            policyProvider,
            resourceAuthorizer,
            stepUpAuth,
            approvalService,
            rateLimiter,
            clock,
            auditWriter);

        // Setup principal with all possible resource scopes so only the permission check controls the outcome
        var fullScope = new ResourceScope(
            new HashSet<string> { "sheet_1" },
            new HashSet<string> { "space_1" },
            new HashSet<string> { "org_1" },
            new HashSet<string> { "acc_1" });

        var principal = PrincipalBuilder.For("user_test")
            .WithRoles(roleName)
            .WithScope(fullScope)
            .StepUpAt(clock.UtcNow)
            .Build();

        principalAccessor.Current = principal;

        var request = new AuthorizationRequest(
            "test_op",
            permissionStr,
            "fingerprint_test_123",
            "provider_test",
            "acc_1",
            new Dictionary<string, string> { ["spreadsheetId"] = "sheet_1", ["paymentAccountId"] = "acc_1" },
            "approval_123");

        approvalService.SeedApproval("approval_123", "fingerprint_test_123", "different_requester");

        var decision = await authzService.AuthorizeAsync(request);

        decision.IsAllowed.ShouldBe(expectedAllow,
            $"Role '{roleName}' with permission '{permissionStr}' expected allowed = {expectedAllow}, but got {decision.Outcome} (Reason: {decision.ReasonCode})");
    }
}
