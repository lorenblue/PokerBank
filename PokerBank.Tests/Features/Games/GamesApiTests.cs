using System.Net;
using System.Net.Http.Json;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Games;

public sealed class GamesApiTests
{
    [Fact]
    public async Task CreateGame_ReturnsCreatedGame()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync("/games", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        Assert.NotNull(game);
        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal("Open", game.Status);
        Assert.NotEqual(default, game.CreatedAtUtc);
        Assert.Equal($"/games/{game.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ListGames_ReturnsGamesNewestFirst()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var olderGame = await CreateGame(client);
        await Task.Delay(10);
        var newerGame = await CreateGame(client);

        var response = await client.GetAsync("/games");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var games = await response.Content.ReadFromJsonAsync<GameResponse[]>();

        Assert.NotNull(games);
        Assert.Collection(
            games,
            game => Assert.Equal(newerGame.Id, game.Id),
            game => Assert.Equal(olderGame.Id, game.Id));
    }

    [Fact]
    public async Task GetGame_ReturnsGame_WhenGameExists()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var createdGame = await CreateGame(client);

        var response = await client.GetAsync($"/games/{createdGame.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        Assert.Equal(createdGame, game);
    }

    [Fact]
    public async Task GetGame_ReturnsNotFound_WhenGameDoesNotExist()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<GameResponse> CreateGame(HttpClient client)
    {
        var response = await client.PostAsync("/games", content: null);
        response.EnsureSuccessStatusCode();

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        return game ?? throw new InvalidOperationException("Create game response was empty.");
    }

    private sealed record GameResponse(Guid Id, string Status, DateTime CreatedAtUtc);
}
