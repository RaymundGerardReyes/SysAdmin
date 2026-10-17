using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Pipeline.Architecture.Tests;

[Trait("Category", "Architecture")]
public class ProviderIsolationTests
{
    [Fact]
    public void ConnectorClasses_AreInternal()
    {
        var providerAssemblies = new[]
        {
            typeof(Integrations.Google.Sheets.SheetsConnector).Assembly,
            typeof(Integrations.Google.Gmail.GmailConnector).Assembly,
            typeof(Integrations.Google.Chat.ChatConnector).Assembly,
            typeof(Integrations.Zoho.Expense.ZohoExpenseConnector).Assembly,
            typeof(Integrations.Zoho.Bigin.BiginConnector).Assembly,
            typeof(Integrations.PayMongo.PayMongoConnector).Assembly
        };

        foreach (var asm in providerAssemblies)
        {
            var connectorTypes = asm.GetTypes()
                .Where(t => t.Name.EndsWith("Connector", StringComparison.Ordinal))
                .ToList();

            connectorTypes.ShouldNotBeEmpty();
            foreach (var type in connectorTypes)
            {
                type.IsNotPublic.ShouldBeTrue($"Connector class {type.FullName} must be internal (C1)");
            }
        }
    }

    [Fact]
    public void ProviderModules_DoNotReference_EachOther()
    {
        var sheetsAssembly = typeof(Integrations.Google.Sheets.SheetsConnector).Assembly;

        var result = Types.InAssembly(sheetsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Integrations.Google.Gmail",
                "Integrations.Google.Chat",
                "Integrations.Zoho.Expense",
                "Integrations.Zoho.Bigin",
                "Integrations.PayMongo")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
