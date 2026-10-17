using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Application.Pipeline.Behaviors;
using Pipeline.Application.UseCases;
using Pipeline.Application.UseCases.Sheets;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;
using Pipeline.Domain.Security;
using Pipeline.Infrastructure.Persistence;
using Pipeline.TestKit.Builders;
using Pipeline.TestKit.Fakes;
using Shouldly;
using Xunit;

namespace Pipeline.Application.Tests.Pipeline;

[RequiresPermission("sheets.values.read")]
public class FakeOrderTestSheetHandler : IUseCaseHandler<ReadSheetRangeCommand, SheetRangeResponse>
{
    public virtual Task<Result<SheetRangeResponse>> HandleAsync(ReadSheetRangeCommand request, UseCaseContext context, CancellationToken ct = default)
        => Task.FromResult(Result<SheetRangeResponse>.Success(new SheetRangeResponse("A1:B2", new List<IReadOnlyList<string>>())));
}

[Trait("Category", "Unit")]
public class DispatcherOrderTests
{
    [Fact]
    public async Task DeniedRequest_NeverReachesHandler()
    {
        var services = new ServiceCollection();
        var handlerMock = Substitute.For<FakeOrderTestSheetHandler>();
        services.AddSingleton<IUseCaseHandler<ReadSheetRangeCommand, SheetRangeResponse>>(handlerMock);

        var principalAccessor = new InMemoryPrincipalAccessor
        {
            // Viewer has NO sheets.values.write, but let's test a handler that is denied
            Current = PrincipalBuilder.For("alice").WithRoles(RoleCatalog.ViewerRole).Build()
        };

        var clock = new FakeClock();
        var auditWriter = new InMemoryAuditWriter();
        var jobStore = new SqliteJobStore();
        var rateLimiter = new RecordingRateLimiter();
        var policyProvider = new InMemoryPolicyBundleProvider { Current = PolicyBundleFactory.CreateInMemoryBundle() };

        var authzService = new AuthorizationService(
            principalAccessor,
            policyProvider,
            new ScopedResourceAuthorizer(),
            new InMemoryStepUpAuthenticator(),
            new InMemoryApprovalService(),
            rateLimiter,
            clock,
            auditWriter);

        var authnBehavior = new AuthenticationBehavior(principalAccessor, clock);
        var validationBehavior = new ValidationBehavior();
        var authzBehavior = new AuthorizationBehavior(authzService);
        var rateLimitBehavior = new RateLimitBehavior(rateLimiter, principalAccessor);
        var idempotencyBehavior = new IdempotencyBehavior(jobStore);
        var auditBehavior = new AuditBehavior(auditWriter, clock);

        var sp = services.BuildServiceProvider();
        var dispatcher = new UseCaseDispatcher(
            sp,
            authnBehavior,
            validationBehavior,
            authzBehavior,
            rateLimitBehavior,
            idempotencyBehavior,
            auditBehavior,
            principalAccessor);

        // ReadSheetRangeCommand requires sheets.values.read, but let's set principal with NO roles
        principalAccessor.Current = PrincipalBuilder.For("alice").WithRoles().Build();

        var cmd = new ReadSheetRangeCommand("sheet_1", "A1:B2");
        var result = await dispatcher.DispatchAsync<ReadSheetRangeCommand, SheetRangeResponse>(cmd);

        result.IsFailure.ShouldBeTrue();
        result.ErrorCode.ShouldBe("PERMISSION_MISSING");

        // Assert handler was NEVER reached!
        await handlerMock.DidNotReceiveWithAnyArgs().HandleAsync(default!, default!);
    }

    [Fact]
    public async Task HandlerException_IsMappedToStableError_AndAudited()
    {
        var services = new ServiceCollection();
        var handlerMock = Substitute.For<FakeOrderTestSheetHandler>();
        handlerMock.HandleAsync(Arg.Any<ReadSheetRangeCommand>(), Arg.Any<UseCaseContext>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Result<SheetRangeResponse>>(new InvalidOperationException("Explosive database crash!")));

        services.AddSingleton<IUseCaseHandler<ReadSheetRangeCommand, SheetRangeResponse>>(handlerMock);

        var principalAccessor = new InMemoryPrincipalAccessor
        {
            Current = PrincipalBuilder.For("alice").WithRoles(RoleCatalog.ViewerRole).WithSpreadsheet("sheet_1").Build()
        };

        var clock = new FakeClock();
        var auditWriter = new InMemoryAuditWriter();
        var jobStore = new SqliteJobStore();
        var rateLimiter = new RecordingRateLimiter();
        var policyProvider = new InMemoryPolicyBundleProvider { Current = PolicyBundleFactory.CreateInMemoryBundle() };

        var authzService = new AuthorizationService(
            principalAccessor,
            policyProvider,
            new ScopedResourceAuthorizer(),
            new InMemoryStepUpAuthenticator(),
            new InMemoryApprovalService(),
            rateLimiter,
            clock,
            auditWriter);

        var authnBehavior = new AuthenticationBehavior(principalAccessor, clock);
        var validationBehavior = new ValidationBehavior();
        var authzBehavior = new AuthorizationBehavior(authzService);
        var rateLimitBehavior = new RateLimitBehavior(rateLimiter, principalAccessor);
        var idempotencyBehavior = new IdempotencyBehavior(jobStore);
        var auditBehavior = new AuditBehavior(auditWriter, clock);

        var sp = services.BuildServiceProvider();
        var dispatcher = new UseCaseDispatcher(
            sp,
            authnBehavior,
            validationBehavior,
            authzBehavior,
            rateLimitBehavior,
            idempotencyBehavior,
            auditBehavior,
            principalAccessor);

        var cmd = new ReadSheetRangeCommand("sheet_1", "A1:B2");
        var result = await dispatcher.DispatchAsync<ReadSheetRangeCommand, SheetRangeResponse>(cmd);

        // Never leaks raw exception details
        result.IsFailure.ShouldBeTrue();
        result.ErrorCode.ShouldBe("INTERNAL_DENY");
        result.ErrorMessage!.ShouldNotContain("Explosive database crash");
        result.ErrorMessage.ShouldBe("Internal processing error occurred.");

        // Audit entry recorded
        auditWriter.Entries.ShouldContain(e => e.Outcome == "Failure" && e.ReasonCode == "INTERNAL_DENY");
    }
}
