using Microsoft.Extensions.DependencyInjection;
using Pipeline.Desktop.Host.Composition;

namespace Pipeline.Desktop.Host;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddPipelineHostServices();
        using var sp = services.BuildServiceProvider();

        // Run fail-closed startup validation
        await StartupValidation.RunAsync(sp);

        Console.WriteLine("Pipeline Desktop Host initialized securely.");
    }
}
