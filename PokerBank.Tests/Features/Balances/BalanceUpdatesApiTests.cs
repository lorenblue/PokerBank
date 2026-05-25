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

        var alice = await CreatePlayer(client, "Alice", "alice@example.com");
        var maya = await CreatePlayer(client, "Maya", emailAddress: null);

        var game = await CreateGame(client);
        await AddBuyIn(client, game.Id, alice.Id, 100m);
        await AddBuyIn(client, game.Id, maya.Id, 100m);
        await AddCashOut(client, game.Id, alice.Id, 160m);
        await AddCashOut(client, game.Id, maya.Id, 40m);
        await CloseGame(client, game.Id);

        await RecordPaymentReceivedByPlayer(client, alice.Id, 20m);

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

        var player = await CreatePlayer(client, "Alice", "alice@example.com");
        await ArchivePlayer(client, player.Id);

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

    private static async Task<PlayerResponse> CreatePlayer(
        HttpClient client,
        string name,
        string? emailAddress)
    {
        var response = await client.PostAsJsonAsync("/players", new { Name = name, EmailAddress = emailAddress });
        response.EnsureSuccessStatusCode();

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        return player ?? throw new InvalidOperationException("Create player response was empty.");
    }

    private static async Task<GameResponse> CreateGame(HttpClient client)
    {
        var response = await client.PostAsync("/games", content: null);
        response.EnsureSuccessStatusCode();

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        return game ?? throw new InvalidOperationException("Create game response was empty.");
    }

    private static async Task AddBuyIn(HttpClient client, Guid gameId, Guid playerId, decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/buy-ins",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();
    }

    private static async Task AddCashOut(HttpClient client, Guid gameId, Guid playerId, decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/cash-outs",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();
    }

    private static async Task CloseGame(HttpClient client, Guid gameId)
    {
        var response = await client.PostAsync($"/games/{gameId}/close", content: null);
        response.EnsureSuccessStatusCode();
    }

    private static async Task RecordPaymentReceivedByPlayer(HttpClient client, Guid playerId, decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/players/{playerId}/payments/received",
            new { Amount = amount, Method = "ETransfer" });
        response.EnsureSuccessStatusCode();
    }

    private static async Task ArchivePlayer(HttpClient client, Guid playerId)
    {
        var response = await client.PostAsync($"/players/{playerId}/archive", content: null);
        response.EnsureSuccessStatusCode();
    }

    private sealed record PlayerResponse(Guid Id, string Name, string? EmailAddress, bool IsActive);

    private sealed record GameResponse(Guid Id, string Status, DateTime CreatedAtUtc);

    private sealed record SendBalanceUpdatesResponse(int Sent, SkippedPlayerResponse[] Skipped);

    private sealed record SkippedPlayerResponse(Guid PlayerId, string PlayerName, string Reason);
}
