using Pipeline.Application.Abstractions;
using Pipeline.Application.Authorization;
using Pipeline.Application.Pipeline;
using Pipeline.Contracts.Responses;
using Pipeline.Domain.Common;

namespace Pipeline.Application.UseCases.Gmail;

public sealed record SendGmailCommand(
    string RecipientEmail,
    string Subject,
    string Body,
    string IdempotencyKey) : IUseCaseRequest
{
    public string OperationName => "gmail.send";
}

[RequiresPermission("gmail.messages.send")]
public sealed class SendGmailMessageUseCase : IUseCaseHandler<SendGmailCommand, GmailMessageResponse>
{
    private readonly ICredentialBroker _credentialBroker;
    private readonly IClock _clock;

    public SendGmailMessageUseCase(ICredentialBroker credentialBroker, IClock clock)
    {
        _credentialBroker = credentialBroker;
        _clock = clock;
    }

    public async Task<Result<GmailMessageResponse>> HandleAsync(
        SendGmailCommand request,
        UseCaseContext context,
        CancellationToken ct = default)
    {
        var token = await _credentialBroker.GetAccessTokenAsync(context.Ticket, "Gmail", "default", ct).ConfigureAwait(false);

        var msgId = $"msg_{Guid.NewGuid():N}";
        var threadId = $"th_{Guid.NewGuid():N}";
        return Result<GmailMessageResponse>.Success(new GmailMessageResponse(msgId, threadId, _clock.UtcNow));
    }
}
