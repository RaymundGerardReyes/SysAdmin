using Pipeline.Application.Authorization;
using Pipeline.Infrastructure.Security;
using Pipeline.TestKit.Fakes;
using Shouldly;
using Xunit;

namespace Pipeline.Application.Tests.Authorization;

[Trait("Category", "Unit")]
public class AuthorizationTicketTests
{
    private readonly FakeClock _clock = new();
    private readonly InMemoryCredentialStore _store = new();
    private readonly InMemoryAuditWriter _auditWriter = new();

    private CredentialBroker CreateBroker() =>
        new(_store, _clock, _auditWriter);

    [Fact]
    public void Ticket_IsBoundTo_OperationFingerprint()
    {
        var ticket = new AuthorizationTicket("op", "fp_123", "Sheets", "acc_1", _clock.UtcNow.AddMinutes(5));
        ticket.OperationFingerprint.ShouldBe("fp_123");
        ticket.VerifyMac().ShouldBeTrue();
    }

    [Fact]
    public void Ticket_ModifiedField_FailsMac()
    {
        var ticket = new AuthorizationTicket("op", "fp_123", "Sheets", "acc_1", _clock.UtcNow.AddMinutes(5));
        // Verify with different key or altered content
        var wrongKey = new byte[32];
        ticket.VerifyMac(wrongKey).ShouldBeFalse();
    }

    [Fact]
    public async Task Ticket_ExpiresAfterTtl()
    {
        var broker = CreateBroker();
        var ticket = new AuthorizationTicket("op", "fp_123", "Sheets", "acc_1", _clock.UtcNow.AddMinutes(5));

        // Advance clock past TTL
        _clock.Advance(TimeSpan.FromMinutes(6));

        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
        {
            await broker.GetAccessTokenAsync(ticket, "Sheets", "acc_1");
        });
    }

    [Fact]
    public async Task Ticket_SecondUse_IsRejected()
    {
        var broker = CreateBroker();
        var ticket = new AuthorizationTicket("op", "fp_123", "Sheets", "acc_1", _clock.UtcNow.AddMinutes(5));

        // First use: succeeds
        var token = await broker.GetAccessTokenAsync(ticket, "Sheets", "acc_1");
        token.ShouldNotBeNull();

        // Second use (replay): fails!
        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
        {
            await broker.GetAccessTokenAsync(ticket, "Sheets", "acc_1");
        });
    }

    [Fact]
    public async Task Ticket_WrongProvider_IsRejected()
    {
        var broker = CreateBroker();
        var ticket = new AuthorizationTicket("op", "fp_123", "GoogleSheets", "acc_1", _clock.UtcNow.AddMinutes(5));

        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
        {
            await broker.GetAccessTokenAsync(ticket, "Gmail", "acc_1");
        });
    }
}
