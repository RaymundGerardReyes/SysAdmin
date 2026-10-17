using NSubstitute;
using Pipeline.Application.Pipeline;
using Pipeline.Application.UseCases.Bigin;
using Pipeline.Contracts.Responses;
using Pipeline.Desktop.Host.Security;
using Pipeline.Domain.Common;
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

    [Fact]
    public void IpcAllowedCommand_BiginModulesRead_IsValid()
    {
        var json = "{\"id\": \"msg_1\", \"command\": \"bigin.modules.read\", \"payload\": {\"dataCenter\": \"US\"}}";
        var valid = IpcSchemaValidator.ValidateEnvelope(json, out var envelope, out var error);

        valid.ShouldBeTrue();
        error.ShouldBeNull();
        envelope.ShouldNotBeNull();
        envelope.Command.ShouldBe("bigin.modules.read");
    }

    [Fact]
    public async Task IpcMessageRouter_RoutesBiginModulesRead_Successfully()
    {
        var dispatcher = Substitute.For<IUseCaseDispatcher>();
        var sampleModules = new List<BiginModuleItemResponse>
        {
            new("2034020000000000129", "Contacts", "Contacts", "Contact", "Contacts", true, true, true, true, true, true, 1, "default", 3, true)
        };
        var expectedContract = new BiginModulesResponseContract(sampleModules);

        dispatcher.DispatchAsync<GetBiginModulesCommand, BiginModulesResponseContract>(
            Arg.Is<GetBiginModulesCommand>(c => c.DataCenter == "US"),
            Arg.Any<CancellationToken>())
            .Returns(Result<BiginModulesResponseContract>.Success(expectedContract));

        var router = new IpcMessageRouter(dispatcher);
        var json = "{\"id\": \"msg_42\", \"command\": \"bigin.modules.read\", \"payload\": {\"dataCenter\": \"US\"}}";

        var response = await router.RouteMessageAsync(json);

        response.ShouldNotBeNull();
        response.Id.ShouldBe("msg_42");
        response.Success.ShouldBeTrue();
        response.Data.ShouldBe(expectedContract);
        response.ErrorCode.ShouldBeNull();
    }

    [Theory]
    [InlineData("bigin.org.read")]
    [InlineData("bigin.org.photo.upload")]
    [InlineData("bigin.relatedlists.read")]
    public void IpcAllowedCommands_BiginOrgAndRelatedList_AreValid(string command)
    {
        var json = $"{{\"id\": \"msg_99\", \"command\": \"{command}\", \"payload\": {{}}}}";
        var valid = IpcSchemaValidator.ValidateEnvelope(json, out var envelope, out var error);

        valid.ShouldBeTrue();
        error.ShouldBeNull();
        envelope.ShouldNotBeNull();
        envelope.Command.ShouldBe(command);
    }

    [Fact]
    public async Task IpcMessageRouter_PhotoUpload_HandlesEmptyPayload_WithoutCrashing()
    {
        var dispatcher = Substitute.For<IUseCaseDispatcher>();
        dispatcher.DispatchAsync<UploadBiginOrgPhotoCommand, BiginPhotoUploadResponseContract>(
            Arg.Any<UploadBiginOrgPhotoCommand>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<BiginPhotoUploadResponseContract>.Failure("EMPTY_FILE", "File bytes cannot be null or empty."));

        var router = new IpcMessageRouter(dispatcher);
        var json = "{\"id\": \"msg_empty_photo\", \"command\": \"bigin.org.photo.upload\", \"payload\": {}}";

        var response = await router.RouteMessageAsync(json);

        response.ShouldNotBeNull();
        response.Id.ShouldBe("msg_empty_photo");
        response.Success.ShouldBeFalse();
        response.ErrorCode.ShouldBe("EMPTY_FILE");
    }

    [Fact]
    public async Task IpcMessageRouter_PhotoUpload_HandlesInvalidBase64_ReturnsInvalidBase64()
    {
        var dispatcher = Substitute.For<IUseCaseDispatcher>();
        var router = new IpcMessageRouter(dispatcher);
        var json = "{\"id\": \"msg_bad_b64\", \"command\": \"bigin.org.photo.upload\", \"payload\": {\"fileBytes\": \"not_valid_base64!@#$\"}}";

        var response = await router.RouteMessageAsync(json);

        response.ShouldNotBeNull();
        response.Id.ShouldBe("msg_bad_b64");
        response.Success.ShouldBeFalse();
        response.ErrorCode.ShouldBe("INVALID_BASE64");
    }

    [Fact]
    public async Task IpcMessageRouter_RelatedListsRead_HandlesEmptyPayload_WithoutCrashing()
    {
        var dispatcher = Substitute.For<IUseCaseDispatcher>();
        dispatcher.DispatchAsync<GetBiginRelatedRecordsCommand, BiginRelatedRecordsResponseContract>(
            Arg.Any<GetBiginRelatedRecordsCommand>(),
            Arg.Any<CancellationToken>())
            .Returns(Result<BiginRelatedRecordsResponseContract>.Failure("INVALID_MODULE", "Module API name is required."));

        var router = new IpcMessageRouter(dispatcher);
        var json = "{\"id\": \"msg_empty_rel\", \"command\": \"bigin.relatedlists.read\", \"payload\": {}}";

        var response = await router.RouteMessageAsync(json);

        response.ShouldNotBeNull();
        response.Id.ShouldBe("msg_empty_rel");
        response.Success.ShouldBeFalse();
        response.ErrorCode.ShouldBe("INVALID_MODULE");
    }
}
