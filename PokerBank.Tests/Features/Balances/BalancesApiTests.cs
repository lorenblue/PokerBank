using System.Net;
using System.Net.Http.Json;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Balances;

[Collection(ApiTestCollection.Name)]
public sealed class BalancesApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ListBalances_ReturnsPlayerBalances()
    {
        using var client = factory.CreateHttpsClient();

        var alice = await CreatePlayer(client, "Alice");
        var lorenzo = await CreatePlayer(client, "Lorenzo");
        var maya = await CreatePlayer(client, "Maya");

        var closedGame = await CreateGame(client);
        await AddBuyIn(client, closedGame.Id, lorenzo.Id, 100m);
        await AddBuyIn(client, closedGame.Id, maya.Id, 100m);
        await AddCashOut(client, closedGame.Id, lorenzo.Id, 160m);
        await AddCashOut(client, closedGame.Id, maya.Id, 40m);
        await CloseGame(client, closedGame.Id);

        var openGame = await CreateGame(client);
        await AddBuyIn(client, openGame.Id, lorenzo.Id, 999m);

        await RecordPayment(client, lorenzo.Id, 20m, "ReceivedByPlayer");
        await RecordPayment(client, maya.Id, 10m, "MadeByPlayer");

        var response = await client.GetAsync("/balances");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balances = await response.Content.ReadFromJsonAsync<BalanceResponse[]>();

        Assert.NotNull(balances);
        Assert.Collection(
            balances,
            balance =>
            {
                Assert.Equal(alice.Id, balance.PlayerId);
                Assert.Equal("Alice", balance.PlayerName);
                Assert.True(balance.IsActive);
                Assert.Equal(0m, balance.GameNetAmount);
                Assert.Equal(0m, balance.PaymentNetAmount);
                Assert.Equal(0m, balance.BalanceAmount);
            },
            balance =>
            {
                Assert.Equal(lorenzo.Id, balance.PlayerId);
                Assert.Equal("Lorenzo", balance.PlayerName);
                Assert.True(balance.IsActive);
                Assert.Equal(60m, balance.GameNetAmount);
                Assert.Equal(20m, balance.PaymentNetAmount);
                Assert.Equal(40m, balance.BalanceAmount);
            },
            balance =>
            {
                Assert.Equal(maya.Id, balance.PlayerId);
                Assert.Equal("Maya", balance.PlayerName);
                Assert.True(balance.IsActive);
                Assert.Equal(-60m, balance.GameNetAmount);
                Assert.Equal(-10m, balance.PaymentNetAmount);
                Assert.Equal(-50m, balance.BalanceAmount);
            });
    }

    [Fact]
    public async Task ListBalances_IncludesArchivedPlayers()
    {
        using var client = factory.CreateHttpsClient();

        var player = await CreatePlayer(client, "Lorenzo");
        await ArchivePlayer(client, player.Id);

        var response = await client.GetAsync("/balances");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balances = await response.Content.ReadFromJsonAsync<BalanceResponse[]>();

        Assert.NotNull(balances);
        var balance = Assert.Single(balances);
        Assert.Equal(player.Id, balance.PlayerId);
        Assert.False(balance.IsActive);
    }

    [Fact]
    public async Task ListBalances_FiltersByPlayer()
    {
        using var client = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(client, "Lorenzo");
        var maya = await CreatePlayer(client, "Maya");

        var game = await CreateGame(client);
        await AddBuyIn(client, game.Id, lorenzo.Id, 100m);
        await AddBuyIn(client, game.Id, maya.Id, 100m);
        await AddCashOut(client, game.Id, lorenzo.Id, 160m);
        await AddCashOut(client, game.Id, maya.Id, 40m);
        await CloseGame(client, game.Id);

        await RecordPayment(client, lorenzo.Id, 20m, "ReceivedByPlayer");
        await RecordPayment(client, maya.Id, 10m, "MadeByPlayer");

        var response = await client.GetAsync($"/balances?playerId={lorenzo.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var balances = await response.Content.ReadFromJsonAsync<BalanceResponse[]>();

        Assert.NotNull(balances);
        var balance = Assert.Single(balances);
        Assert.Equal(lorenzo.Id, balance.PlayerId);
        Assert.Equal("Lorenzo", balance.PlayerName);
        Assert.True(balance.IsActive);
        Assert.Equal(60m, balance.GameNetAmount);
        Assert.Equal(20m, balance.PaymentNetAmount);
        Assert.Equal(40m, balance.BalanceAmount);
    }

    private static async Task<PlayerResponse> CreatePlayer(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/players", new { Name = name });
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

    private static async Task RecordPayment(HttpClient client, Guid playerId, decimal amount, string direction)
    {
        var response = await client.PostAsJsonAsync(
            PaymentUrl(playerId, direction),
            new { Amount = amount, Method = "ETransfer" });
        response.EnsureSuccessStatusCode();
    }

    private static string PaymentUrl(Guid playerId, string direction) =>
        direction switch
        {
            "MadeByPlayer" => $"/players/{playerId}/payments/made",
            "ReceivedByPlayer" => $"/players/{playerId}/payments/received",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown payment direction.")
        };

    private static async Task ArchivePlayer(HttpClient client, Guid playerId)
    {
        var response = await client.PostAsync($"/players/{playerId}/archive", content: null);
        response.EnsureSuccessStatusCode();
    }

    private sealed record PlayerResponse(Guid Id, string Name, bool IsActive);

    private sealed record GameResponse(Guid Id, string Status, DateTime CreatedAtUtc);

    private sealed record BalanceResponse(
        Guid PlayerId,
        string PlayerName,
        bool IsActive,
        decimal GameNetAmount,
        decimal PaymentNetAmount,
        decimal BalanceAmount);
}
