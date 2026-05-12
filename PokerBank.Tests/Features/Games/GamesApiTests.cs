using System.Net;
using System.Net.Http.Json;
using PokerBank.Tests.TestSupport;

namespace PokerBank.Tests.Features.Games;

[Collection(ApiTestCollection.Name)]
public sealed class GamesApiTests(PokerBankApiFactory factory) : IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateGame_ReturnsCreatedGame()
    {
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
        using var client = factory.CreateHttpsClient();

        var createdGame = await CreateGame(client);

        var response = await client.GetAsync($"/games/{createdGame.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var game = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(game);
        Assert.Equal(createdGame.Id, game.Id);
        Assert.Equal(createdGame.Status, game.Status);
        AssertCloseTo(createdGame.CreatedAtUtc, game.CreatedAtUtc);
        Assert.Empty(game.Entries);
    }

    [Fact]
    public async Task GetGame_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_ReturnsEntries_WhenGameHasBuyIns()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        var buyIn = await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.GetAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var gameDetails = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        var entry = Assert.Single(gameDetails.Entries);
        Assert.Equal(buyIn.Id, entry.Id);
        Assert.Equal(player.Id, entry.PlayerId);
        Assert.Equal(50m, entry.Amount);
        Assert.Equal("BuyIn", entry.Type);
        AssertCloseTo(buyIn.RecordedAtUtc, entry.RecordedAtUtc);
    }

    [Fact]
    public async Task GetGame_ReturnsEntries_WhenGameHasCashOuts()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 75m);
        var cashOut = await AddCashOut(client, game.Id, player.Id, 50m);

        var response = await client.GetAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var gameDetails = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        var entry = Assert.Single(gameDetails.Entries, entry => entry.Id == cashOut.Id);
        Assert.Equal(player.Id, entry.PlayerId);
        Assert.Equal(50m, entry.Amount);
        Assert.Equal("CashOut", entry.Type);
        AssertCloseTo(cashOut.RecordedAtUtc, entry.RecordedAtUtc);
    }

    [Fact]
    public async Task AddBuyIn_ReturnsCreatedEntry()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/buy-ins",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var entry = await response.Content.ReadFromJsonAsync<GameEntryResponse>();

        Assert.NotNull(entry);
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(game.Id, entry.GameId);
        Assert.Equal(player.Id, entry.PlayerId);
        Assert.Equal(50m, entry.Amount);
        Assert.Equal("BuyIn", entry.Type);
        Assert.NotEqual(default, entry.RecordedAtUtc);
        Assert.Equal($"/games/{game.Id}/entries/{entry.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AddBuyIn_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            $"/games/{Guid.NewGuid()}/buy-ins",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddBuyIn_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/buy-ins",
            new { PlayerId = Guid.NewGuid(), Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task AddBuyIn_ReturnsBadRequest_WhenAmountIsNotPositive(decimal amount)
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/buy-ins",
            new { PlayerId = player.Id, Amount = amount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddBuyIn_ReturnsConflict_WhenGameIsClosed()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);
        await CloseGame(client, game.Id);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/buy-ins",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsCreatedEntry()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 75m);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var entry = await response.Content.ReadFromJsonAsync<GameEntryResponse>();

        Assert.NotNull(entry);
        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(game.Id, entry.GameId);
        Assert.Equal(player.Id, entry.PlayerId);
        Assert.Equal(50m, entry.Amount);
        Assert.Equal("CashOut", entry.Type);
        Assert.NotEqual(default, entry.RecordedAtUtc);
        Assert.Equal($"/games/{game.Id}/entries/{entry.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task AddCashOut_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            $"/games/{Guid.NewGuid()}/cash-outs",
            new { PlayerId = player.Id, Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsNotFound_WhenPlayerDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = Guid.NewGuid(), Amount = 50m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task AddCashOut_ReturnsBadRequest_WhenAmountIsNotPositive(decimal amount)
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = player.Id, Amount = amount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsConflict_WhenCashOutsWouldExceedBuyIns()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = player.Id, Amount = 50.01m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsConflict_WhenGameIsClosed()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);
        await CloseGame(client, game.Id);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = player.Id, Amount = 1m });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CloseGame_ReturnsOk_WhenGameIsBalanced()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);

        var response = await client.PostAsync($"/games/{game.Id}/close", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var closedGame = await response.Content.ReadFromJsonAsync<GameResponse>();

        Assert.NotNull(closedGame);
        Assert.Equal(game.Id, closedGame.Id);
        Assert.Equal("Closed", closedGame.Status);
        AssertCloseTo(game.CreatedAtUtc, closedGame.CreatedAtUtc);

        var getResponse = await client.GetAsync($"/games/{game.Id}");
        getResponse.EnsureSuccessStatusCode();

        var gameDetails = await getResponse.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        Assert.Equal("Closed", gameDetails.Status);
    }

    [Fact]
    public async Task CloseGame_ReturnsNotFound_WhenGameDoesNotExist()
    {
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsync($"/games/{Guid.NewGuid()}/close", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CloseGame_ReturnsConflict_WhenGameIsNotBalanced()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.PostAsync($"/games/{game.Id}/close", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CloseGame_ReturnsConflict_WhenGameIsAlreadyClosed()
    {
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);
        await AddCashOut(client, game.Id, player.Id, 50m);
        await CloseGame(client, game.Id);

        var response = await client.PostAsync($"/games/{game.Id}/close", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static async Task<GameResponse> CreateGame(HttpClient client)
    {
        var response = await client.PostAsync("/games", content: null);
        response.EnsureSuccessStatusCode();

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        return game ?? throw new InvalidOperationException("Create game response was empty.");
    }

    private static async Task<PlayerResponse> CreatePlayer(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/players", new { Name = name });
        response.EnsureSuccessStatusCode();

        var player = await response.Content.ReadFromJsonAsync<PlayerResponse>();

        return player ?? throw new InvalidOperationException("Create player response was empty.");
    }

    private static async Task<GameEntryResponse> AddBuyIn(
        HttpClient client,
        Guid gameId,
        Guid playerId,
        decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/buy-ins",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();

        var entry = await response.Content.ReadFromJsonAsync<GameEntryResponse>();

        return entry ?? throw new InvalidOperationException("Add buy-in response was empty.");
    }

    private static async Task<GameEntryResponse> AddCashOut(
        HttpClient client,
        Guid gameId,
        Guid playerId,
        decimal amount)
    {
        var response = await client.PostAsJsonAsync(
            $"/games/{gameId}/cash-outs",
            new { PlayerId = playerId, Amount = amount });
        response.EnsureSuccessStatusCode();

        var entry = await response.Content.ReadFromJsonAsync<GameEntryResponse>();

        return entry ?? throw new InvalidOperationException("Add cash-out response was empty.");
    }

    private static async Task<GameResponse> CloseGame(HttpClient client, Guid gameId)
    {
        var response = await client.PostAsync($"/games/{gameId}/close", content: null);
        response.EnsureSuccessStatusCode();

        var game = await response.Content.ReadFromJsonAsync<GameResponse>();

        return game ?? throw new InvalidOperationException("Close game response was empty.");
    }

    private static void AssertCloseTo(DateTime expected, DateTime actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }

    private static void AssertCloseTo(DateTimeOffset expected, DateTimeOffset actual)
    {
        Assert.InRange((actual - expected).Duration(), TimeSpan.Zero, TimeSpan.FromMilliseconds(1));
    }

    private sealed record GameResponse(Guid Id, string Status, DateTime CreatedAtUtc);

    private sealed record GameDetailsResponse(
        Guid Id,
        string Status,
        DateTime CreatedAtUtc,
        GameEntryDetailsResponse[] Entries);

    private sealed record PlayerResponse(Guid Id, string Name, bool IsActive);

    private sealed record GameEntryDetailsResponse(
        Guid Id,
        Guid PlayerId,
        decimal Amount,
        string Type,
        DateTimeOffset RecordedAtUtc);

    private sealed record GameEntryResponse(
        Guid Id,
        Guid GameId,
        Guid PlayerId,
        decimal Amount,
        string Type,
        DateTimeOffset RecordedAtUtc);
}
