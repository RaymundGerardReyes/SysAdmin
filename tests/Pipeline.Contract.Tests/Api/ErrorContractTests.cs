using System.Reflection;
using Pipeline.Contracts.Errors;
using Shouldly;
using Xunit;

namespace Pipeline.Contract.Tests.Api;

[Trait("Category", "Contract")]
public class ErrorContractTests
{
    [Fact]
    public void AllErrorCodes_AreStableAndDocumented()
    {
        var fields = typeof(ErrorCodes).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .ToList();

        fields.ShouldNotBeEmpty();

        foreach (var f in fields)
        {
            var val = (string)f.GetValue(null)!;
            val.ShouldNotBeNullOrWhiteSpace();
            val.ShouldBe(val.ToUpperInvariant(), "Error codes must be SCREAMING_SNAKE_CASE");
        }
    }

    [Fact]
    public void ErrorBodies_NeverContainSecretsOrStackTraces()
    {
        var error = new ProblemDetailsContract(
            "https://pipeline.io/errors/PERMISSION_MISSING",
            "Permission Missing",
            403,
            "User does not hold the required permission to execute this operation.",
            "/api/v1/jobs",
            ErrorCodes.PermissionMissing,
            "corr_12345");

        var text = error.ToString();
        text.ShouldNotContain("Bearer ");
        text.ShouldNotContain("access_token");
        text.ShouldNotContain("StackTrace");
        text.ShouldNotContain("at Pipeline.");
    }
}
