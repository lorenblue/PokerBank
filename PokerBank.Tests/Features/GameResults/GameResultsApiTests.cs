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

        var lorenzo = await client.CreatePlayerAsync("Lorenzo");
        var maya = await client.CreatePlayerAsync("Maya");

        var game = await client.CreateGameAsync();
        await client.AddBuyInAsync(game.Id, lorenzo.Id, 100m);
        await client.AddBuyInAsync(game.Id, maya.Id, 100m);
        await client.AddCashOutAsync(game.Id, lorenzo.Id, 160m);
        await client.AddCashOutAsync(game.Id, maya.Id, 40m);
        await client.CloseGameAsync(game.Id);

        var openGame = await client.CreateGameAsync();
        await client.AddBuyInAsync(openGame.Id, lorenzo.Id, 999m);

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

        var lorenzo = await client.CreatePlayerAsync("Lorenzo");
        var maya = await client.CreatePlayerAsync("Maya");

        var winningGame = await client.CreateGameAsync();
        await client.AddBuyInAsync(winningGame.Id, lorenzo.Id, 100m);
        await client.AddBuyInAsync(winningGame.Id, maya.Id, 100m);
        await client.AddCashOutAsync(winningGame.Id, lorenzo.Id, 160m);
        await client.AddCashOutAsync(winningGame.Id, maya.Id, 40m);
        await client.CloseGameAsync(winningGame.Id);

        await Task.Delay(10);

        var losingGame = await client.CreateGameAsync();
        await client.AddBuyInAsync(losingGame.Id, lorenzo.Id, 75m);
        await client.AddBuyInAsync(losingGame.Id, lorenzo.Id, 25m);
        await client.AddBuyInAsync(losingGame.Id, maya.Id, 25m);
        await client.AddCashOutAsync(losingGame.Id, lorenzo.Id, 20m);
        await client.AddCashOutAsync(losingGame.Id, lorenzo.Id, 30m);
        await client.AddCashOutAsync(losingGame.Id, maya.Id, 75m);
        await client.CloseGameAsync(losingGame.Id);

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

        var lorenzo = await client.CreatePlayerAsync("Lorenzo");
        var maya = await client.CreatePlayerAsync("Maya");

        var firstGame = await client.CreateGameAsync();
        await client.AddBuyInAsync(firstGame.Id, lorenzo.Id, 100m);
        await client.AddBuyInAsync(firstGame.Id, maya.Id, 100m);
        await client.AddCashOutAsync(firstGame.Id, lorenzo.Id, 160m);
        await client.AddCashOutAsync(firstGame.Id, maya.Id, 40m);
        await client.CloseGameAsync(firstGame.Id);

        var secondGame = await client.CreateGameAsync();
        await client.AddBuyInAsync(secondGame.Id, lorenzo.Id, 50m);
        await client.AddBuyInAsync(secondGame.Id, maya.Id, 50m);
        await client.AddCashOutAsync(secondGame.Id, lorenzo.Id, 25m);
        await client.AddCashOutAsync(secondGame.Id, maya.Id, 75m);
        await client.CloseGameAsync(secondGame.Id);

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

        var lorenzo = await client.CreatePlayerAsync("Lorenzo");
        var maya = await client.CreatePlayerAsync("Maya");

        var game = await client.CreateGameAsync();
        await client.AddBuyInAsync(game.Id, lorenzo.Id, 100m);
        await client.AddBuyInAsync(game.Id, maya.Id, 100m);
        await client.AddCashOutAsync(game.Id, lorenzo.Id, 160m);
        await client.AddCashOutAsync(game.Id, maya.Id, 40m);
        await client.CloseGameAsync(game.Id);

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

    private static void AssertCloseTo(DateTime expected, DateTime actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }
}
