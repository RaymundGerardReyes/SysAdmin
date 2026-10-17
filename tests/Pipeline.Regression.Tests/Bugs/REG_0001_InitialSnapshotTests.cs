using Shouldly;
using Xunit;

namespace Pipeline.Regression.Tests.Bugs;

[Trait("Category", "Regression")]
public class REG_0001_InitialSnapshotTests
{
    [Fact]
    public void REG_0001_InitialBaselineSanity()
    {
        // Baseline regression test proving test host functions
        true.ShouldBeTrue();
    }
}
