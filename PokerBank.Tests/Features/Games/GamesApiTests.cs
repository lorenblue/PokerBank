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

        var game = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(game);
        Assert.Equal(createdGame.Id, game.Id);
        Assert.Equal(createdGame.Status, game.Status);
        Assert.Equal(createdGame.CreatedAtUtc, game.CreatedAtUtc);
        Assert.Empty(game.Entries);
    }

    [Fact]
    public async Task GetGame_ReturnsNotFound_WhenGameDoesNotExist()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync($"/games/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGame_ReturnsEntries_WhenGameHasBuyIns()
    {
        await using var factory = new PokerBankApiFactory();
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
        Assert.Equal(buyIn.RecordedAtUtc, entry.RecordedAtUtc);
    }

    [Fact]
    public async Task GetGame_ReturnsEntries_WhenGameHasCashOuts()
    {
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 75m);
        var cashOut = await AddCashOut(client, game.Id, player.Id, 50m);

        var response = await client.GetAsync($"/games/{game.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var gameDetails = await response.Content.ReadFromJsonAsync<GameDetailsResponse>();

        Assert.NotNull(gameDetails);
        Assert.Contains(gameDetails.Entries, entry =>
            entry.Id == cashOut.Id &&
            entry.PlayerId == player.Id &&
            entry.Amount == 50m &&
            entry.Type == "CashOut" &&
            entry.RecordedAtUtc == cashOut.RecordedAtUtc);
    }

    [Fact]
    public async Task AddBuyIn_ReturnsCreatedEntry()
    {
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/buy-ins",
            new { PlayerId = player.Id, Amount = amount });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddCashOut_ReturnsCreatedEntry()
    {
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
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
        await using var factory = new PokerBankApiFactory();
        using var client = factory.CreateHttpsClient();

        var game = await CreateGame(client);
        var player = await CreatePlayer(client, "Lorenzo");
        await AddBuyIn(client, game.Id, player.Id, 50m);

        var response = await client.PostAsJsonAsync(
            $"/games/{game.Id}/cash-outs",
            new { PlayerId = player.Id, Amount = 50.01m });

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
