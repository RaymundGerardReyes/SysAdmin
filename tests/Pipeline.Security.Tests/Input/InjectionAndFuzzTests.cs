using Pipeline.Desktop.Host.Security;
using Shouldly;
using Xunit;

namespace Pipeline.Security.Tests.Input;

[Trait("Category", "Security")]
public class InjectionAndFuzzTests
{
    [Fact]
    public void IpcUnknownCommand_IsRejected()
    {
        var json = "{\"id\": \"1\", \"command\": \"eval.arbitrary.code\", \"payload\": {}}";
        var valid = IpcSchemaValidator.ValidateEnvelope(json, out var envelope, out var error);

        valid.ShouldBeFalse();
        error.ShouldBe("UNKNOWN_COMMAND");
        envelope.ShouldBeNull();
    }

    [Fact]
    public void IpcMalformedJson_ReturnsStableError_NoCrash()
    {
        var malformed = "{ id: 1, command: 'broken";
        var valid = IpcSchemaValidator.ValidateEnvelope(malformed, out var envelope, out var error);

        valid.ShouldBeFalse();
        error.ShouldBe("MALFORMED_JSON");
        envelope.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{}")]
    [InlineData("{\"id\": \"1\"}")]
    public void IpcIncompleteEnvelope_IsRejected(string payload)
    {
        var valid = IpcSchemaValidator.ValidateEnvelope(payload, out _, out var error);
        valid.ShouldBeFalse();
        error.ShouldNotBeNull();
    }
}
