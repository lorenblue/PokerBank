using System.Net;
using System.Net.Http.Json;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.GameResults;

[Collection(ApiTestCollection.Name)]
public sealed class GameResultsApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ListGameResults_ReturnsClosedGameResults()
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

        var openGame = await CreateGame(client);
        await AddBuyIn(client, openGame.Id, lorenzo.Id, 999m);

        var response = await client.GetAsync("/game-results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<GameResultResponse[]>();

        Assert.NotNull(results);
        Assert.Collection(
            results,
            result =>
            {
                Assert.Equal(lorenzo.Id, result.PlayerId);
                Assert.Equal("Lorenzo", result.PlayerName);
                Assert.Equal(game.Id, result.GameId);
                AssertCloseTo(game.CreatedAtUtc, result.PlayedAtUtc);
                Assert.Equal(100m, result.BuyInAmount);
                Assert.Equal(160m, result.CashOutAmount);
                Assert.Equal(60m, result.NetAmount);
            },
            result =>
            {
                Assert.Equal(maya.Id, result.PlayerId);
                Assert.Equal("Maya", result.PlayerName);
                Assert.Equal(game.Id, result.GameId);
                AssertCloseTo(game.CreatedAtUtc, result.PlayedAtUtc);
                Assert.Equal(100m, result.BuyInAmount);
                Assert.Equal(40m, result.CashOutAmount);
                Assert.Equal(-60m, result.NetAmount);
            });
    }

    [Fact]
    public async Task ListGameResults_FiltersByPlayer()
    {
        using var client = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(client, "Lorenzo");
        var maya = await CreatePlayer(client, "Maya");

        var winningGame = await CreateGame(client);
        await AddBuyIn(client, winningGame.Id, lorenzo.Id, 100m);
        await AddBuyIn(client, winningGame.Id, maya.Id, 100m);
        await AddCashOut(client, winningGame.Id, lorenzo.Id, 160m);
        await AddCashOut(client, winningGame.Id, maya.Id, 40m);
        await CloseGame(client, winningGame.Id);

        await Task.Delay(10);

        var losingGame = await CreateGame(client);
        await AddBuyIn(client, losingGame.Id, lorenzo.Id, 75m);
        await AddBuyIn(client, losingGame.Id, lorenzo.Id, 25m);
        await AddBuyIn(client, losingGame.Id, maya.Id, 25m);
        await AddCashOut(client, losingGame.Id, lorenzo.Id, 20m);
        await AddCashOut(client, losingGame.Id, lorenzo.Id, 30m);
        await AddCashOut(client, losingGame.Id, maya.Id, 75m);
        await CloseGame(client, losingGame.Id);

        var response = await client.GetAsync($"/game-results?playerId={lorenzo.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<GameResultResponse[]>();

        Assert.NotNull(results);
        Assert.Collection(
            results,
            result =>
            {
                Assert.Equal(lorenzo.Id, result.PlayerId);
                Assert.Equal("Lorenzo", result.PlayerName);
                Assert.Equal(losingGame.Id, result.GameId);
                AssertCloseTo(losingGame.CreatedAtUtc, result.PlayedAtUtc);
                Assert.Equal(100m, result.BuyInAmount);
                Assert.Equal(50m, result.CashOutAmount);
                Assert.Equal(-50m, result.NetAmount);
            },
            result =>
            {
                Assert.Equal(lorenzo.Id, result.PlayerId);
                Assert.Equal("Lorenzo", result.PlayerName);
                Assert.Equal(winningGame.Id, result.GameId);
                AssertCloseTo(winningGame.CreatedAtUtc, result.PlayedAtUtc);
                Assert.Equal(100m, result.BuyInAmount);
                Assert.Equal(160m, result.CashOutAmount);
                Assert.Equal(60m, result.NetAmount);
            });
    }

    [Fact]
    public async Task ListGameResults_FiltersByGame()
    {
        using var client = factory.CreateHttpsClient();

        var lorenzo = await CreatePlayer(client, "Lorenzo");
        var maya = await CreatePlayer(client, "Maya");

        var firstGame = await CreateGame(client);
        await AddBuyIn(client, firstGame.Id, lorenzo.Id, 100m);
        await AddBuyIn(client, firstGame.Id, maya.Id, 100m);
        await AddCashOut(client, firstGame.Id, lorenzo.Id, 160m);
        await AddCashOut(client, firstGame.Id, maya.Id, 40m);
        await CloseGame(client, firstGame.Id);

        var secondGame = await CreateGame(client);
        await AddBuyIn(client, secondGame.Id, lorenzo.Id, 50m);
        await AddBuyIn(client, secondGame.Id, maya.Id, 50m);
        await AddCashOut(client, secondGame.Id, lorenzo.Id, 25m);
        await AddCashOut(client, secondGame.Id, maya.Id, 75m);
        await CloseGame(client, secondGame.Id);

        var response = await client.GetAsync($"/game-results?gameId={secondGame.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<GameResultResponse[]>();

        Assert.NotNull(results);
        Assert.Collection(
            results,
            result =>
            {
                Assert.Equal(lorenzo.Id, result.PlayerId);
                Assert.Equal("Lorenzo", result.PlayerName);
                Assert.Equal(secondGame.Id, result.GameId);
                AssertCloseTo(secondGame.CreatedAtUtc, result.PlayedAtUtc);
                Assert.Equal(50m, result.BuyInAmount);
                Assert.Equal(25m, result.CashOutAmount);
                Assert.Equal(-25m, result.NetAmount);
            },
            result =>
            {
                Assert.Equal(maya.Id, result.PlayerId);
                Assert.Equal("Maya", result.PlayerName);
                Assert.Equal(secondGame.Id, result.GameId);
                AssertCloseTo(secondGame.CreatedAtUtc, result.PlayedAtUtc);
                Assert.Equal(50m, result.BuyInAmount);
                Assert.Equal(75m, result.CashOutAmount);
                Assert.Equal(25m, result.NetAmount);
            });
    }

    [Fact]
    public async Task ListGameResults_FiltersByPlayerAndGame()
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

        var response = await client.GetAsync($"/game-results?playerId={maya.Id}&gameId={game.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var results = await response.Content.ReadFromJsonAsync<GameResultResponse[]>();

        Assert.NotNull(results);
        var result = Assert.Single(results);
        Assert.Equal(maya.Id, result.PlayerId);
        Assert.Equal("Maya", result.PlayerName);
        Assert.Equal(game.Id, result.GameId);
        AssertCloseTo(game.CreatedAtUtc, result.PlayedAtUtc);
        Assert.Equal(100m, result.BuyInAmount);
        Assert.Equal(40m, result.CashOutAmount);
        Assert.Equal(-60m, result.NetAmount);
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

    private static void AssertCloseTo(DateTime expected, DateTime actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }

    private sealed record PlayerResponse(Guid Id, string Name, bool IsActive);

    private sealed record GameResponse(Guid Id, string Status, DateTime CreatedAtUtc);

    private sealed record GameResultResponse(
        Guid PlayerId,
        string PlayerName,
        Guid GameId,
        DateTime PlayedAtUtc,
        decimal BuyInAmount,
        decimal CashOutAmount,
        decimal NetAmount);
}
