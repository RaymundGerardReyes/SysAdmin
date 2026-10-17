using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Pipeline.Architecture.Tests;

[Trait("Category", "Architecture")]
public class LayerDependencyTests
{
    [Fact]
    public void Domain_DependsOnNothingExternal()
    {
        var domainAssembly = typeof(Domain.Security.Permission).Assembly;

        var result = Types.InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Pipeline.Application",
                "Pipeline.Infrastructure",
                "Pipeline.Desktop.Host",
                "Pipeline.LocalApi",
                "Integrations.Google.Sheets",
                "Integrations.Google.Gmail",
                "Integrations.Google.Chat",
                "Integrations.Zoho.Expense",
                "Integrations.Zoho.Bigin",
                "Integrations.PayMongo")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Application_DoesNotDependOn_Infrastructure_Or_Providers()
    {
        var appAssembly = typeof(Application.Pipeline.IUseCaseDispatcher).Assembly;

        var result = Types.InAssembly(appAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Pipeline.Infrastructure",
                "Pipeline.Desktop.Host",
                "Pipeline.LocalApi",
                "Integrations.Google.Sheets",
                "Integrations.Google.Gmail",
                "Integrations.Google.Chat",
                "Integrations.Zoho.Expense",
                "Integrations.Zoho.Bigin",
                "Integrations.PayMongo")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Host_DoesNotReference_ProviderModules()
    {
        var hostAssembly = typeof(Desktop.Host.Program).Assembly;

        var result = Types.InAssembly(hostAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Integrations.Google.Sheets",
                "Integrations.Google.Gmail",
                "Integrations.Google.Chat",
                "Integrations.Zoho.Expense",
                "Integrations.Zoho.Bigin",
                "Integrations.PayMongo")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(Environment.NewLine, result.FailingTypeNames ?? Array.Empty<string>()));
    }
}
