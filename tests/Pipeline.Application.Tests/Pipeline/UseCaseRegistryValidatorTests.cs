using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Application.UseCases;
using Pipeline.Domain.Common;
using Shouldly;
using Xunit;

namespace Pipeline.Application.Tests.Pipeline;

public sealed record DummyValidCommand : IUseCaseRequest { public string OperationName => "jobs.read"; }
public sealed record DummyUnprotectedCommand : IUseCaseRequest { public string OperationName => "test.op"; }

[RequiresPermission("jobs.read")]
public sealed class ValidHandler : IUseCaseHandler<DummyValidCommand, string>
{
    public Task<Result<string>> HandleAsync(DummyValidCommand request, UseCaseContext context, CancellationToken ct) =>
        Task.FromResult(Result<string>.Success("ok"));
}

// Missing attribute
public sealed class UnprotectedHandler : IUseCaseHandler<DummyUnprotectedCommand, string>
{
    public Task<Result<string>> HandleAsync(DummyUnprotectedCommand request, UseCaseContext context, CancellationToken ct) =>
        Task.FromResult(Result<string>.Success("bad"));
}

// Unknown permission
[RequiresPermission("malicious.backdoor.execute")]
public sealed class UnknownPermissionHandler : IUseCaseHandler<DummyUnprotectedCommand, string>
{
    public Task<Result<string>> HandleAsync(DummyUnprotectedCommand request, UseCaseContext context, CancellationToken ct) =>
        Task.FromResult(Result<string>.Success("bad"));
}

[Trait("Category", "Unit")]
public class UseCaseRegistryValidatorTests
{
    [Fact]
    public void HandlerWithoutAttribute_Throws()
    {
        var types = new[] { typeof(UnprotectedHandler) };
        var ex = Should.Throw<InvalidOperationException>(() => UseCaseRegistryValidator.ValidateOrThrow(types));
        ex.Message.ShouldContain("missing mandatory [RequiresPermission]");
    }

    [Fact]
    public void HandlerWithUnknownPermission_Throws()
    {
        var types = new[] { typeof(UnknownPermissionHandler) };
        var ex = Should.Throw<InvalidOperationException>(() => UseCaseRegistryValidator.ValidateOrThrow(types));
        ex.Message.ShouldContain("unknown permission");
    }

    [Fact]
    public void AllHandlersProperlyDeclared_DoesNotThrow()
    {
        var types = new[] { typeof(ValidHandler) };
        Should.NotThrow(() => UseCaseRegistryValidator.ValidateOrThrow(types));
    }
}
