using System.Net;
using System.Net.Http.Json;
using PokerBank.Domain;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Balances;

[Collection(ApiTestCollection.Name)]
public sealed class BalanceUpdatesApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SendBalanceUpdates_SendsEmailsToActivePlayersWithEmailAddresses()
    {
        using var client = factory.CreateHttpsClient();

        var alice = await client.CreatePlayerAsync("Alice", "alice@example.com");
        var maya = await client.CreatePlayerAsync("Maya", emailAddress: null);

        var game = await client.CreateGameAsync();
        await client.AddBuyInAsync(game.Id, alice.Id, 100m);
        await client.AddBuyInAsync(game.Id, maya.Id, 100m);
        await client.AddCashOutAsync(game.Id, alice.Id, 160m);
        await client.AddCashOutAsync(game.Id, maya.Id, 40m);
        await client.CloseGameAsync(game.Id);

        await client.RecordPaymentReceivedByPlayerAsync(alice.Id, 20m);

        var response = await client.PostAsync("/balances/updates/send", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SendBalanceUpdatesResponse>();

        Assert.NotNull(result);
        Assert.Equal(1, result.Sent);

        var skipped = Assert.Single(result.Skipped);
        Assert.Equal(maya.Id, skipped.PlayerId);
        Assert.Equal("Maya", skipped.PlayerName);
        Assert.Equal("Missing email address", skipped.Reason);

        var email = Assert.Single(factory.SentEmails);
        Assert.Equal("alice@example.com", email.To);
        Assert.Equal("PokerBank balance update", email.Subject);
        Assert.Contains("Hi Alice", email.Body);
        Assert.Contains("You should receive $40.00.", email.Body);
        Assert.Contains("Game net: +$60.00", email.Body);
        Assert.Contains("Payment net: +$20.00", email.Body);
    }

    [Fact]
    public async Task SendBalanceUpdates_DoesNotSendEmailsToArchivedPlayers()
    {
        using var client = factory.CreateHttpsClient();

        var player = await client.CreatePlayerAsync("Alice", "alice@example.com");
        await client.ArchivePlayerAsync(player.Id);

        var response = await client.PostAsync("/balances/updates/send", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<SendBalanceUpdatesResponse>();

        Assert.NotNull(result);
        Assert.Equal(0, result.Sent);
        Assert.Empty(result.Skipped);
        Assert.Empty(factory.SentEmails);
    }

    [Fact]
    public async Task SendBalanceUpdates_ReturnsUnauthorized_WhenUserIsNotSignedIn()
    {
        using var client = factory.CreateUnauthenticatedHttpsClient();

        var response = await client.PostAsync("/balances/updates/send", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SendBalanceUpdates_ReturnsForbidden_WhenUserIsMember()
    {
        await factory.SetDefaultAdminRoleAsync(GroupRole.Member);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync("/balances/updates/send", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(factory.SentEmails);
    }

}
