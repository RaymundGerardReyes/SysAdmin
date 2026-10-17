using Microsoft.Extensions.DependencyInjection;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Application.Pipeline.Behaviors;
using Pipeline.Application.UseCases;
using Pipeline.Application.UseCases.Admin;
using Pipeline.Application.UseCases.Payments;
using Pipeline.Application.UseCases.Sheets;
using Pipeline.Contracts.Errors;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Security;
using Pipeline.Infrastructure.Persistence;
using Pipeline.TestKit.Builders;
using Pipeline.TestKit.Fakes;
using Shouldly;
using Xunit;

namespace Pipeline.Security.Tests.Rbac;

[Trait("Category", "Security")]
public class PrivilegeEscalationTests
{
    private readonly InMemoryPrincipalAccessor _principalAccessor = new();
    private readonly InMemoryAuditWriter _auditWriter = new();
    private readonly FakeClock _clock = new();
    private readonly InMemoryApprovalService _approvalService = new();
    private readonly SqliteJobStore _jobStore = new();
    private readonly RecordingRateLimiter _rateLimiter = new();

    private IUseCaseDispatcher CreateDispatcher(IServiceProvider sp)
    {
        var policy = new InMemoryPolicyBundleProvider { Current = PolicyBundleFactory.CreateInMemoryBundle() };
        var authzService = new AuthorizationService(
            _principalAccessor,
            policy,
            new ScopedResourceAuthorizer(),
            new InMemoryStepUpAuthenticator { AlwaysFresh = true },
            _approvalService,
            _rateLimiter,
            _clock,
            _auditWriter);

        return new UseCaseDispatcher(
            sp,
            new AuthenticationBehavior(_principalAccessor, _clock),
            new ValidationBehavior(),
            new AuthorizationBehavior(authzService),
            new RateLimitBehavior(_rateLimiter, _principalAccessor),
            new IdempotencyBehavior(_jobStore),
            new AuditBehavior(_auditWriter, _clock),
            _principalAccessor);
    }

    [Fact]
    public async Task SEC_RBAC_ViewerCallsSheetsWrite_Denied()
    {
        var services = new ServiceCollection();
        services.AddTransient<IUseCaseHandler<WriteSheetRangeCommand, SheetWriteResponse>, WriteSheetRangeUseCase>();
        services.AddSingleton<ICredentialBroker, CredentialBrokerStub>();

        var sp = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(sp);

        _principalAccessor.Current = PrincipalBuilder.For("viewer_user")
            .WithRoles(RoleCatalog.ViewerRole)
            .Build();

        var cmd = new WriteSheetRangeCommand("sheet_1", "A1", new List<IReadOnlyList<string>> { new[] { "hack" } }, "key_1");
        var result = await dispatcher.DispatchAsync<WriteSheetRangeCommand, SheetWriteResponse>(cmd);

        result.IsFailure.ShouldBeTrue();
        result.ErrorCode.ShouldBe(ErrorCodes.PermissionMissing);
    }

    [Fact]
    public async Task SEC_RBAC_OperatorCallsPaymentsCreate_Denied()
    {
        var services = new ServiceCollection();
        services.AddTransient<IUseCaseHandler<CreatePaymentCheckoutCommand, PaymentCheckoutResponse>, CreatePaymentCheckoutUseCase>();
        services.AddSingleton<ICredentialBroker, CredentialBrokerStub>();

        var sp = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(sp);

        _principalAccessor.Current = PrincipalBuilder.For("operator_user")
            .WithRoles(RoleCatalog.OperatorRole)
            .Build();

        var cmd = new CreatePaymentCheckoutCommand("acc_1", 1000m, "PHP", "ref", "key_pay");
        var result = await dispatcher.DispatchAsync<CreatePaymentCheckoutCommand, PaymentCheckoutResponse>(cmd);

        result.IsFailure.ShouldBeTrue();
        result.ErrorCode.ShouldBe(ErrorCodes.PermissionMissing);
    }

    [Fact]
    public async Task SEC_RBAC_OwnerChangesOwnRoles_Denied()
    {
        var services = new ServiceCollection();
        services.AddTransient<IUseCaseHandler<ManageUserRolesCommand, ManageUserRolesResponse>, ManageUserRolesUseCase>();

        var sp = services.BuildServiceProvider();
        var dispatcher = CreateDispatcher(sp);

        _principalAccessor.Current = PrincipalBuilder.For("owner_user")
            .WithRoles(RoleCatalog.OwnerRole)
            .Build();

        // Target is self
        var cmd = new ManageUserRolesCommand("owner_user", new HashSet<string> { RoleCatalog.OwnerRole, RoleCatalog.FinanceOperatorRole });
        var result = await dispatcher.DispatchAsync<ManageUserRolesCommand, ManageUserRolesResponse>(cmd);

        result.IsFailure.ShouldBeTrue();
        result.ErrorCode.ShouldBe(ErrorCodes.SodViolation);
    }
}

internal sealed class CredentialBrokerStub : ICredentialBroker
{
    public Task<AccessToken> GetAccessTokenAsync(AuthorizationTicket ticket, string provider, string accountId, CancellationToken ct = default) =>
        Task.FromResult(new AccessToken("test_token", DateTimeOffset.UtcNow.AddHours(1)));
}
