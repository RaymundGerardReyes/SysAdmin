using Pipeline.Domain.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Domain.Tests.Security;

[Trait("Category", "Unit")]
public class PermissionTests
{
    [Theory]
    [InlineData("jobs.read")]
    [InlineData("jobs.cancel")]
    [InlineData("sheets.values.read")]
    [InlineData("sheets.values.write")]
    [InlineData("gmail.messages.send")]
    [InlineData("payments.checkout.create")]
    public void ValidPermission_ConstructsSuccessfully(string value)
    {
        var permission = new Permission(value);
        permission.Value.ShouldBe(value);
        permission.ToString().ShouldBe(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Jobs.Read")]
    [InlineData("jobs")]
    [InlineData("jobs.read.write.extra")]
    [InlineData("jobs..read")]
    [InlineData("jobs.read;")]
    [InlineData("jobs-read")]
    public void InvalidPermission_ThrowsArgumentException(string invalid)
    {
        Should.Throw<ArgumentException>(() => new Permission(invalid));
    }
}
