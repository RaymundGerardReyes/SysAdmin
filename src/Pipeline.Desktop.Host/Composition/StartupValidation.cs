using Microsoft.Extensions.DependencyInjection;
using Pipeline.Application.Abstractions;
using Pipeline.Application.Pipeline;
using Pipeline.Application.UseCases;

namespace Pipeline.Desktop.Host.Composition;

public static class StartupValidation
{
    public static async Task RunAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        // 1. Scan use cases and validate with UseCaseRegistryValidator
        var handlers = typeof(IUseCaseHandler).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IUseCaseHandler).IsAssignableFrom(t))
            .ToList();

        UseCaseRegistryValidator.ValidateOrThrow(handlers);

        // 2. Verify audit chain
        var auditWriter = sp.GetRequiredService<IAuditWriter>();
        var status = await auditWriter.VerifyChainAsync(ct).ConfigureAwait(false);
        if (!status.IsIntact)
        {
            throw new InvalidOperationException($"Audit chain broken at sequence {status.FirstBrokenSequence}. Halting startup.");
        }
    }
}
