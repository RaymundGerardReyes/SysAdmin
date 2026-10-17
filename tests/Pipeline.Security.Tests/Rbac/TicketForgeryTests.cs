using Pipeline.Application.Authorization;
using Pipeline.Infrastructure.Security;
using Pipeline.TestKit.Fakes;
using Shouldly;
using Xunit;

namespace Pipeline.Security.Tests.Rbac;

[Trait("Category", "Security")]
public class TicketForgeryTests
{
    private readonly FakeClock _clock = new();
    private readonly InMemoryCredentialStore _store = new();
    private readonly InMemoryAuditWriter _audit = new();

    private CredentialBroker CreateBroker() => new(_store, _clock, _audit);

    [Fact]
    public async Task SEC_TICKET_BrokerCalledWithoutTicket_Refused()
    {
        var broker = CreateBroker();
        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
        {
            await broker.GetAccessTokenAsync(null!, "Sheets", "acc_1");
        });
    }

    [Fact]
    public async Task SEC_TICKET_ReplayedTicket_Refused()
    {
        var broker = CreateBroker();
        var ticket = new AuthorizationTicket("op", "fp_1", "Sheets", "acc_1", _clock.UtcNow.AddMinutes(5));

        // First call
        var token = await broker.GetAccessTokenAsync(ticket, "Sheets", "acc_1");
        token.ShouldNotBeNull();

        // Replay attempt
        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
        {
            await broker.GetAccessTokenAsync(ticket, "Sheets", "acc_1");
        });
    }

    [Fact]
    public async Task SEC_TICKET_TicketFromOtherOperation_Refused()
    {
        var broker = CreateBroker();
        var ticket = new AuthorizationTicket("sheets.read", "fp_1", "Sheets", "acc_1", _clock.UtcNow.AddMinutes(5));

        // Attempting to use a Sheets ticket to extract PayMongo credentials
        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
        {
            await broker.GetAccessTokenAsync(ticket, "PayMongo", "acc_1");
        });
    }

    [Fact]
    public async Task SEC_TICKET_ExpiredTicket_Refused()
    {
        var broker = CreateBroker();
        var ticket = new AuthorizationTicket("op", "fp_1", "Sheets", "acc_1", _clock.UtcNow.AddMinutes(1));
        _clock.Advance(TimeSpan.FromMinutes(2));

        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
        {
            await broker.GetAccessTokenAsync(ticket, "Sheets", "acc_1");
        });
    }
}
